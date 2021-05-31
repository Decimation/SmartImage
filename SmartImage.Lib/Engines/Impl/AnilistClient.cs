using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SimpleCore.Net;

// ReSharper disable UnusedMember.Global

namespace SmartImage.Lib.Engines.Impl
{
	public sealed class AnilistClient
	{
		private readonly SimpleGraphQLClient m_client;

		public AnilistClient()
		{
			m_client = new SimpleGraphQLClient("https://graphql.anilist.co");
		}

		public string GetTitle(int anilistId)
		{
			/*
			 * https://anilist.gitbook.io/anilist-apiv2-docs/overview/graphql
			 * https://anilist.gitbook.io/anilist-apiv2-docs/overview/graphql/getting-started
			 * https://graphql.org/learn/queries/
			 */
			const string graphQuery = @"query ($id: Int) { # Define which variables will be used in the query (id)
				Media(id: $id, type: ANIME) { # Insert our variables into the query arguments (id) (type: ANIME is hard-coded in the query)
					id
					title {
						romaji
						english
						native
					}
				}
			}";


			var response = (JObject) m_client.Execute(graphQuery, new
			{
				query = graphQuery,
				id    = anilistId
			});

			return response["data"]["Media"]["title"]["english"].ToString();
		}
	}
}