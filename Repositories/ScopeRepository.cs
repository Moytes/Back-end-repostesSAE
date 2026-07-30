using Dapper;
using Npgsql;

namespace Back_end_RepostesSAE.Repositories;

public sealed class ScopeRepository(IConfiguration configuration) : IScopeRepository
{
    private readonly string _connectionString =
        configuration.GetConnectionString("ReportsDb")
        ?? throw new InvalidOperationException("Connection string 'ReportsDb' is not configured.");

    public async Task<int[]> GetAllowedSchoolIds(Guid userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT us.school_id
            FROM user_school us
            WHERE us.user_id = @UserId
            UNION
            SELECT s.id
            FROM "school" s
            JOIN "user" u ON u.id = @UserId
            WHERE u.school_zone_id IS NOT NULL AND s.school_zone_id = u.school_zone_id;
            """;

        var ids = await conn.QueryAsync<int>(sql, new { UserId = userId });
        return ids.Distinct().ToArray();
    }

    public async Task<bool> IsStudentInScope(Guid studentId, int[] allowedSchoolIds, int[] attentionAreaIds)
    {
        if (allowedSchoolIds.Length == 0)
            return false;

        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM "student" s
                LEFT JOIN "registration" r ON r.student_id = s.id
                LEFT JOIN "group" g ON g.id = r.group_id
                WHERE s.id = @StudentId
                  AND COALESCE(g.school_id, s.school_id) = ANY(@AllowedSchoolIds)
                  AND EXISTS (
                      SELECT 1 FROM "student_attention_area" saa
                      WHERE saa.student_id = s.id AND saa.attention_area_id = ANY(@AreaIds)
                  )
            );
            """;

        return await conn.ExecuteScalarAsync<bool>(sql, new
        {
            StudentId = studentId,
            AllowedSchoolIds = allowedSchoolIds,
            AreaIds = attentionAreaIds
        });
    }
}
