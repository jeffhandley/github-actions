using Octokit.GraphQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Octokit.GraphQL.Variable;

namespace Labeler.Queries;

internal class TeamQueries
{
    public static async Task<bool> QueryIsTeamMember(Connection connection, string owner, string team, string user)
    {
        IEnumerable<string> teamMembers = await QueryTeamMembers(connection, owner, team, user);

        return teamMembers.Any(member => string.Equals(member, user, StringComparison.InvariantCultureIgnoreCase));
    }

    public static async Task<IEnumerable<string>> QueryTeamMembers(Connection connection, string owner, string team, string? query)
    {
        var teamMembersQuery = new Query()
            .Organization(Var("owner"))
            .Team(Var("team"))
            .Members(query: Var("query"))
            .AllPages()
            .Select(user => user.Login)
            .Compile();

        var teamMembers = await connection.Run(teamMembersQuery, new Dictionary<string, object?>
            {
                { "owner", owner },
                { "team", team },
                { "query", query }
            });

        return teamMembers;
    }
}
