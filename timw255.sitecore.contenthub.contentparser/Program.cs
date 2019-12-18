using FileHelpers;
using Stylelabs.M.Base.Querying;
using System.Linq;
using Stylelabs.M.Base.Querying.Linq;
using Stylelabs.M.Framework.Essentials.LoadOptions;
using Stylelabs.M.Sdk;
using Stylelabs.M.Sdk.WebClient;
using Stylelabs.M.Sdk.WebClient.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using timw255.sitecore.contenthub.contentparser.Model;
using System.Globalization;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.Exceptions;
using Stylelabs.M.Sdk.Models.Jobs;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace timw255.sitecore.contenthub.contentparser
{
    class Program
    {
        private static IWebMClient _client;

        static async Task Main(string[] args)
        {
            var endpoint = new Uri("https://twt-demo.stylelabs.io/");

            var oauth = new OAuthPasswordGrant()
            {
                ClientId = "the_client_id",
                ClientSecret = "the_client_secret",
                UserName = "the_username",
                Password = "the_password"
            };

            _client = MClientFactory.CreateMClient(endpoint, oauth);

            //await CreateAssetsFromCSV();

            await CreateSmartRenditions();

            Console.ReadLine();
        }

        static async Task<bool> CreateAssetsFromCSV()
        {
            // use the FileHelpers library to load data from CSV
            var engine = new FileHelperEngine<Asset>();

            var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Data\data.csv");
            var assets = engine.ReadFile(filePath);

            // import all the things...
            var contentRepositories = await GetDefinitionItems("M.Content.Repository");

            var enUs = CultureInfo.GetCultureInfo("en-US");

            foreach (var asset in assets)
            {
                // creates a new asset (in memory. will not persist until explicitly saved.)
                var newEntity = await _client.EntityFactory.CreateAsync(Constants.Asset.DefinitionName);

                newEntity.SetPropertyValue("FileName", asset.File);
                newEntity.SetPropertyValue("Title", asset.Title);
                newEntity.SetPropertyValue("Description", enUs, asset.Description);

                // assign the asset to a repo
                var contentRepositoryToAssetRelation = newEntity.GetRelation("ContentRepositoryToAsset", RelationRole.Child) as IChildToManyParentsRelation;
                contentRepositoryToAssetRelation.Add((long)contentRepositories.Single(s => s.Identifier == asset.ContentRepositoryToAsset).Id);

                try
                {
                    // persist the asset information
                    var entityId = await _client.Entities.SaveAsync(newEntity);
                    Console.WriteLine(String.Format("Created - Name: {0}, EntityId: {1}", asset.Title, entityId));

                    // set the lifecycle status. (it seems that assets MUST exist prior to this action.)
                    switch (asset.FinalLifeCycleStatusToAsset)
                    {
                        case "M.Final.LifeCycle.Status.UnderReview":
                            await _client.Assets.FinalLifeCycleManager.SubmitAsync(entityId);
                            break;
                        case "M.Final.LifeCycle.Status.Approved":
                            await _client.Assets.FinalLifeCycleManager.DirectPublishAsync(entityId);
                            break;
                    }

                    // fetch jobs still need to be created in order to pull the asset content... otherwise, they will just
                    // sit there, empty and alone.
                    var fetchJobRequest = new WebFetchJobRequest("Fetch file for entity.", entityId);
                    fetchJobRequest.Urls.Add(new Uri(asset.File, UriKind.Absolute));

                    var jobId = await _client.Jobs.CreateFetchJobAsync(fetchJobRequest);
                    Console.WriteLine(String.Format("Created Fetch Job - EntityId: {0} JobId: {1}", entityId, jobId));
                }
                catch (ValidationException e)
                {
                    // sad face...
                    foreach (var failure in e.Failures)
                    {
                        Console.WriteLine(String.Format("Failure - Source: {0}, Message: {1}", failure.Source, failure.Message));
                        return false;
                    }
                }
            }

            return true;
        }

        static async Task<IList<IEntity>> GetDefinitionItems(string definitionName)
        {
            var query = Query.CreateQuery(entities => (from e in entities where e.DefinitionName == definitionName select e));

            var result = await _client.Querying.QueryAsync(query);

            return result.Items;
        }

        static async Task CreateSmartRenditions()
        {
            Console.Write("EntityId: ");
            var entityId = Convert.ToInt64(Console.ReadLine());

            // get the asset from the system so that we can read the focal point info
            var entity = await _client.Entities.GetAsync(entityId);

            if (entity == null)
            {
                // (i kept typing entity ids incorrectly... which caused issues.)
                Console.WriteLine(String.Format("Entity does not exist - Id: {0}", entityId));
                return;
            }
            
            var renditions = new List<SmartRenditionConfig>();

            // what size do we want the custom crops to be?
            renditions.Add(new SmartRenditionConfig() { Width = 700, Height = 700, PreserveFocalArea = false });
            renditions.Add(new SmartRenditionConfig() { Width = 750, Height = 350, PreserveFocalArea = false });

            foreach (var rendition in renditions)
            {
                var mainFile = JObject.Parse(entity.GetPropertyValue("MainFile").ToString());

                // calculate custom crops based on the focal point data
                var config = SmartCrop.GetConversionConfiguration(entity.GetPropertyValue<int>("FocalPointX"),
                    entity.GetPropertyValue<int>("FocalPointY"),
                    entity.GetPropertyValue<int>("FocalPointWidth"),
                    entity.GetPropertyValue<int>("FocalPointHeight"),
                    entity.GetPropertyValue<int>("FocalPointAnchor"),
                    mainFile["properties"]["width"].ToObject<int>(),
                    mainFile["properties"]["height"].ToObject<int>(),
                    rendition.Width, rendition.Height, rendition.PreserveFocalArea);

                // create a public link in memory, again... must be persisted to actually mean anything
                var publicLink = await _client.EntityFactory.CreateAsync(Constants.PublicLink.DefinitionName);
                publicLink.SetPropertyValue(Constants.PublicLink.Resource, "downloadOriginal");

                // tell the public link how to do the custom crop
                publicLink.SetPropertyValue("ConversionConfiguration", config);

                // relate the public link to the entity
                var assetTopublicLinkRelation = publicLink.GetRelation(Constants.PublicLink.AssetToPublicLink, RelationRole.Child) as IChildToManyParentsRelation;
                assetTopublicLinkRelation.Add(entityId);

                // create the link
                await _client.Entities.SaveAsync(publicLink);

                Console.WriteLine("Done.");
            }
        }
    }
}
