using CommandLine;
using System;

namespace WikiToolsShared
{
    public class SiteOptions
    {
        [Option(HelpText = "The wiki site.", Default = "https://magicarena.fandom.com")]
        public string Site { get; set; }

        public virtual void Validate()
        {
            if (string.IsNullOrEmpty(Site))
                return;

            if (!Uri.IsWellFormedUriString(Site, UriKind.Absolute))
                throw new OptionsException($"Site \"{Site}\" is not a valid url.");

            if (Site.EndsWith("/"))
                throw new OptionsException($"Invalid site {Site}. Don't end the site name with a '/'");

            if (!Site.ToUpperInvariant().StartsWith("HTTPS"))
                throw new OptionsException($"Site {Site} is invalid. Only https is allowed. e.g https://mywiki.fandom.com");
        }
    }
}
