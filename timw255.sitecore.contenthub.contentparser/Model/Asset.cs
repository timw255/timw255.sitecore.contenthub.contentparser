using FileHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace timw255.sitecore.contenthub.contentparser.Model
{
    [DelimitedRecord(","), IgnoreFirst(1)]
    public class Asset
    {
        [FieldQuoted('"')]
        public string File;

        [FieldQuoted('"')]
        public string Title;

        [FieldQuoted('"')]
        public string FinalLifeCycleStatusToAsset;

        [FieldQuoted('"')]
        public string ContentRepositoryToAsset;

        [FieldQuoted('"')]
        public string Description;

        [FieldQuoted('"')]
        public string MarketingDescription;

        [FieldQuoted('"')]
        public string AssetTypeToAsset;

        [FieldQuoted('"')]
        public string SocialMediaChannel;

        [FieldQuoted('"')]
        public string ContentSecurity;

        [FieldQuoted('"')]
        public string AssetSource;

        [FieldQuoted('"')]
        public string RightsProfileToAsset;
    }
}
