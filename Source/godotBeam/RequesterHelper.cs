using System;
using System.Collections.Generic;
using System.Text;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Api.Stats;
using Beamable.Common.Content;
using Beamable.Serialization.SmallerJSON;

namespace GodotBeamable.BeamGodot
{
	public static class RequesterHelper
	{
		public static Promise<EmptyResponse> SetStats(this IBeamableRequester requester, long userId, string access,
		                                              Dictionary<string, string> stats)
		{
			string prefix = $"client.{access}.player.";
			return requester.Request<EmptyResponse>(
				Method.POST,
				$"/object/stats/{prefix}{userId}/client/stringlist",
				new StatUpdates(stats)
			);
		}

		public static Promise<BatchReadStatsResponse> GetStats(this IBeamableRequester requester, long userId)
		{
			return requester.Request<BatchReadStatsResponse>(
				Method.GET,
				$"/basic/stats/client/batch?format=stringlist&objectIds=client.public.player.{userId}");
		}

		public static Promise<ClientManifest> GetManifest(this IBeamableRequester requester)
		{
			return requester.Request(Method.GET, "/basic/content/manifest/public?id=global", null, true,
			                         ClientManifest.ParseCSV);
		}

		public static Promise<LeaderBoardView> GetBoardScores(this IBeamableRequester requester, string boardId,
		                                                int fromIndex, int max, long? focus = null,
		                                                long? outlier = null)
		{
			if (string.IsNullOrEmpty(boardId))
			{
				return Promise<LeaderBoardView>.Failed(new Exception("Leaderboard ID cannot be uninitialized."));
			}

			string query = $"from={fromIndex}&max={max}";
			if (focus.HasValue)
			{
				query += $"&focus={focus.Value}";
			}

			if (outlier.HasValue)
			{
				query += $"&outlier={outlier.Value}";
			}

			string encodedBoardId = requester.EscapeURL(boardId);
			return requester.Request<LeaderBoardV2ViewResponse>(
				Method.GET,
				$"/object/leaderboards/{encodedBoardId}/view?{query}"
			).Map(rsp => rsp.lb);
		}


		public static  Promise<LeaderboardAssignmentInfo> GetBoardAssignment(this IBeamableRequester requester,string boardId, bool joinBoard)
		{
			string encodedBoardId = requester.EscapeURL(boardId);
			return requester.Request<LeaderboardAssignmentInfo>(
				Method.GET,
				$"/basic/leaderboards/assignment?boardId={encodedBoardId}&joinBoard={joinBoard}"
			);
		}

		public static Promise<EmptyResponse> UpdateBoardScore(this IBeamableRequester requester, string boardId, long userId,
		                                                      double score, bool increment = false,
		                                                      IDictionary<string, object> stats = null)
		{
			var req = new ArrayDict
			{
				{"score", score},
				{"id", userId},
				{"increment", increment}
			};
			if (stats != null)
			{
				req["stats"] = new ArrayDict(stats);
			}

			var body = Json.Serialize(req, new StringBuilder());
			string encodedBoardId = requester.EscapeURL(boardId);
			return requester.GetBoardAssignment(boardId, true).FlatMap(info => requester.Request<EmptyResponse>(
				                                                           Method.PUT,
				                                                           $"/object/leaderboards/{encodedBoardId}/entry",
				                                                           body
			                                                           ));
		}
	}
}