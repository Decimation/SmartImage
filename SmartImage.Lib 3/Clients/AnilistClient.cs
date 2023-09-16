using System.Json;
using Kantan.Net;
using Newtonsoft.Json.Linq;

// ReSharper disable PossibleNullReferenceException

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Clients;

public sealed class AnilistClient : IDisposable
{
    private readonly GraphQLClient m_client;

    public AnilistClient()
    {
        m_client = new GraphQLClient("https://graphql.anilist.co");
    }

    public async Task<string> GetTitleAsync(int anilistId)
    {
        /*
		 * https://anilist.gitbook.io/anilist-apiv2-docs/overview/graphql
		 * https://anilist.gitbook.io/anilist-apiv2-docs/overview/graphql/getting-started
		 * https://graphql.org/learn/queries/
		 */

        const string GRAPH_QUERY = @"query ($id: Int) { # Define which variables will be used in the query (id)
				Media(id: $id, type: ANIME) { # Insert our variables into the query arguments (id) (type: ANIME is hard-coded in the query)
					id
					title {
						romaji
						english
						native
					}
				}
			}";

        var response = await m_client.ExecuteAsync(GRAPH_QUERY, new
        {
            query = GRAPH_QUERY,
            id = anilistId
        });

        return response["data"]["Media"]["title"]["english"];
    }

    #region IDisposable

    public void Dispose()
    {
        m_client.Dispose();
    }

    #endregion
}