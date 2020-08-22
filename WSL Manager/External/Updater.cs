using Octokit;
using System;
using System.Threading.Tasks;

namespace WSL_Manager.External
{
    class Updater
    {
        private string currentVersion;

        public Updater(String currentVersion)
        {
            this.currentVersion = currentVersion;
        }

        public async Task<String> CheckForUpdateAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue("WSL-Manager"));
            var releases = await client.Repository.Release.GetAll("visdauas", "WSL-Manager");
            var latest = releases[0];
            if (latest.TagName != currentVersion)
                return latest.HtmlUrl;
            else
                return null;
        }
    }
}
