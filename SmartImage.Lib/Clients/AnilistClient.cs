using Kantan.Net;

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

	public async Task<string> GetTitleAsync(long anilistId)
	{
		/*
		 * https://anilist.gitbook.io/anilist-apiv2-docs/overview/graphql
		 * https://anilist.gitbook.io/anilist-apiv2-docs/overview/graphql/getting-started
		 * https://graphql.org/learn/queries/
		 */

		const string GRAPH_QUERY = """
		                           query ($id: Int) { # Define which variables will be used in the query (id)
		                           				Media(id: $id, type: ANIME) { # Insert our variables into the query arguments (id) (type: ANIME is hard-coded in the query)
		                           					id
		                           					title {
		                           						romaji
		                           						english
		                           						native
		                           					}
		                           				}
		                           			}
		                           """;

		var response = await m_client.ExecuteAsync(GRAPH_QUERY, new
		{
			query = GRAPH_QUERY,
			id    = anilistId
		}).ConfigureAwait(false);

		var value = response["data"];
		var title = value?["Media"]?["title"];
		return title?["english"]?.ToString() ?? title?["romaji"]?.ToString();
	}

#region IDisposable

	public void Dispose()
	{

		m_client.Dispose();
	}

#endregion

	/// <summary>
	/// https://anilist.co/anime/{id}/
	/// </summary>
	public const string ANILIST_URL = "https://anilist.co/anime/";

	/// <summary>
	/// Used to retrieve more information about results
	/// </summary>
	public static AnilistClient Instance { get; private set; } = new();

}