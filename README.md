# timw255.sitecore.contenthub.contentparser

Not that it matters, but this is named "contentparser" because it started out as an experiment for creating assets from CSV data... then morphed into something else. Feel free to re-write my messy code. :)

This is a quick and dirty example of using the Sitecore Content Hub Web SDK. (Also contains code for the [focal point extension](https://youtu.be/v9H7BG-aWqQ).)

**For this project to even build, you'll need to [setup MyGet](https://docs-partners.stylelabs.com/content/integrations/web-sdk/getting-started.html).**

## If you'd like to experiment with focal points...

Add the following members to M.Asset.

* FocalPointX
* FocalPointY
* FocalPointWidth
* FocalPointHeight
* FocalPointAnchor

After adding the members, you can add an [external component](https://docs-partners.stylelabs.com/content/integrations/intergration-components/external-page-component/intro.html) to your "Asset Detail Page". The code and template to use can be found in the "ExternalComponentFiles" folder of this repo.

**Notes:**
1. In the [demo video](https://youtu.be/v9H7BG-aWqQ), I hid the default "Entity Image viewer" component so that there was only one visible image on the page.
2. I'm not sure if the URL used by the external component is the "correct" one to use but it worked... so I left it.
3. The console application in this repo will pull the focal point data for an asset, calculate some custom crops, and then create public links. Ideally, something like this would be a [script](https://docs-partners.stylelabs.com/content/integrations/scripting-api/scripting-api-overview.html) and kicked off by a [trigger](https://docs-partners.stylelabs.com/content/integrations/intergration-components/triggers/overview.html) however, I wanted to have some fun with the web sdk...