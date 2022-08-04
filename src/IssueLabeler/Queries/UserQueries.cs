using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Octokit.GraphQL.Variable;

namespace IssueLabeler.Queries
{
    internal class UserQueries
    {
        public static async Task<RepositoryPermission?> QueryUserRepositoryPermission(Connection connection, string owner, string repo, string author)
        {
            var collaboratorQuery = new Query()
                .Repository(Var("repo"), Var("owner"))
                .Collaborators(query: Var("author"))
                .Select(c => new
                {
                    c.PageInfo.HasNextPage,
                    c.PageInfo.EndCursor,
                    Users = c.Edges.Select(e => new
                    {
                        e.Node.Login,
                        e.Permission,
                    }).ToList()
                })
                .Compile();

            while (true)
            {
                string? afterCursor = null;

                var collaborators = await connection.Run(collaboratorQuery, new Dictionary<string, object?>
                {
                    { "owner", owner },
                    { "repo", repo },
                    { "author", author },
                    { "after_cursor", afterCursor }
                });

                var collaborator = collaborators.Users.SingleOrDefault(user => string.Equals(user.Login, author, StringComparison.InvariantCultureIgnoreCase));

                if (collaborator is not null)
                {
                    return collaborator.Permission;
                }

                if (!collaborators.HasNextPage)
                {
                    return null;
                }
                else
                {
                    afterCursor = collaborators.EndCursor;
                }
            }
        }
    }
}
