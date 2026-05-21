using System.Collections.Generic;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Query used for search URLs.
    /// </summary>
    public class LinkQuery
    {
        #region Constructors

        public LinkQuery() { }

        public LinkQuery(string name, string value)
        {
            this.name = name;
            this.value = value;
        }

        #endregion

        #region Values

        /// <summary>
        /// Name of the query.
        /// </summary>
        public string name;

        /// <summary>
        /// Value of the query.
        /// </summary>
        public string value;

        #endregion

        #region Functions

        /// <summary>
        /// Builds a search query.
        /// </summary>
        /// <param name="searchURL">URL to append.</param>
        /// <param name="queries">Array of queries.</param>
        /// <returns>Returns the built query.</returns>
        public static string BuildQuery(string searchURL, List<LinkQuery> queries)
        {
            if (!queries.IsEmpty())
                searchURL += "?";

            for (int i = 0; i < queries.Count; i++)
            {
                searchURL += queries[i].ToString();
                if (i < queries.Count - 1)
                    searchURL += "&";
            }

            return searchURL;
        }

        /// <summary>
        /// Builds a search query.
        /// </summary>
        /// <param name="searchURL">URL to append.</param>
        /// <param name="queries">Array of queries.</param>
        /// <returns>Returns the built query.</returns>
        public static string BuildQuery(string searchURL, params LinkQuery[] queries)
        {
            if (!queries.IsEmpty())
                searchURL += "?";

            for (int i = 0; i < queries.Length; i++)
            {
                searchURL += queries[i].ToString();
                if (i < queries.Length - 1)
                    searchURL += "&";
            }

            return searchURL;
        }

        /// <summary>
        /// Builds a search query.
        /// </summary>
        /// <param name="searchURL">URL to append.</param>
        /// <param name="search">Search query.</param>
        /// <param name="page">Page query.</param>
        /// <param name="sort">Sort query.</param>
        /// <param name="ascend">Ascend query.</param>
        /// <returns>Returns the built query.</returns>
        public static string BuildQuery(string searchURL, string search, int page = 0, int sort = 0, bool ascend = false)
        {
            var queries = new List<LinkQuery>();
            if (!string.IsNullOrEmpty(search))
                queries.Add(new LinkQuery("query", search));
            if (page != 0)
                queries.Add(new LinkQuery("page", page.ToString()));

            if (sort != 0)
                queries.Add(new LinkQuery("sort", sort.ToString()));
            if (ascend)
                queries.Add(new LinkQuery("ascend", ascend.ToString()));

            return BuildQuery(searchURL, queries);
        }

        public override string ToString() => $"{name}={value}";

        #endregion
    }
}
