using System.Web;
using System.Web.Optimization;

namespace Lemuria.Server
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/vendor/bootstrap/css/bootstrap.min.css",
                      "~/vendor/font-awesome/css/font-awesome.min.css",
                      "~/vendor/simple-line-icons/css/simple-line-icons.css",
                      "~/vendor/device-mockups/device-mockups.min.css",
                      "~/Content/new-age.css"));


            bundles.Add(new ScriptBundle("~/bundles/js").Include(
                      "~/vendor/bootstrap/js/bootstrap.min.js",
                      "~/vendor/jquery/jquery.min.js",
                      "~/Scripts/jquery.easing.min.js",
                      "~/Scripts/new-age.js"));

        }
    }
}
