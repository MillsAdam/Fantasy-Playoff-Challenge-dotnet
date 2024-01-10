using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Capstone.Models.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Capstone.DAO.Position.Kicker
{
    public class KLast4TotalSqlDao : IKLast4TotalDao
    {
        private readonly string _connectionString;
        private readonly IConfigurationDao _configurationDao;
        public KLast4TotalSqlDao(IConfiguration configuration, IConfigurationDao configurationDao)
        {
            _connectionString = configuration.GetConnectionString("Project");
            _configurationDao = configurationDao;
        }

        private const string SELECT_SQL = 
            @"WITH StartingWeek AS (
                SELECT week 
                FROM player_stats_ext 
                WHERE week = @week 
                ORDER BY week 
                LIMIT 1
            ) 
            SELECT 
                p.player_id, 
                COUNT(DISTINCT pse.week) AS week, 
                p.position, 
                t.team, 
                p.name, 
                p.status, 
                p.injury_status, 
                SUM(pse.field_goals_made) AS field_goals_made, 
                SUM(pse.field_goals_attempted) AS field_goals_attempted, 
                CASE 
                    WHEN SUM(pse.field_goals_attempted) = 0 THEN 0 
                    ELSE ROUND((SUM(pse.field_goals_made) / SUM(pse.field_goals_attempted)) * 100, 2) 
                END AS field_goal_percentage, 
                SUM(pse.field_goals_made_0_to_19) AS field_goals_made_0_to_19, 
                SUM(pse.field_goals_made_20_to_29) AS field_goals_made_20_to_29, 
                SUM(pse.field_goals_made_30_to_39) AS field_goals_made_30_to_39, 
                SUM(pse.field_goals_made_40_to_49) AS field_goals_made_40_to_49, 
                SUM(pse.field_goals_made_50_plus) AS field_goals_made_50_plus, 
                SUM(pse.extra_points_made) AS extra_points_made, 
                SUM(pse.extra_points_attempted) AS extra_points_attempted, 
                CASE 
                    WHEN SUM(pse.extra_points_attempted) = 0 THEN 0 
                    ELSE ROUND((SUM(pse.extra_points_made) / SUM(pse.extra_points_attempted)) * 100, 2) 
                END AS extra_point_percentage, 
                SUM(pse.fantasy_points) AS fantasy_points_total, 
                ROUND(AVG(pse.fantasy_points), 2) AS fantasy_points_average, 
                t.conference, 
                t.status AS team_status 
            FROM player_stats_ext pse 
            JOIN players p ON p.player_id = pse.player_id 
            JOIN teams t ON t.team_id = pse.team_id 
            CROSS JOIN StartingWeek AS sw 
            WHERE p.position = 'K' 
                AND pse.week <= sw.week 
                AND pse.week >= sw.week - 3 ";

        private const string GROUP_BY_SQL =
            @"GROUP BY 
                p.player_id, 
                p.position, 
                t.team, 
                p.name, 
                p.status, 
                p.injury_status, 
                t.conference, 
                t.status 
            ORDER BY fantasy_points_total DESC;";

        private const string CONF_SQL =
            @"AND lower(t.conference) ILIKE @conf ";

        private const string TEAM_SQL =
            @"AND lower(t.team) ILIKE @team ";

        private const string NAME_SQL =
            @"AND lower(p.name) ILIKE @name ";

        public async Task<List<PlayerStatsExtDto>> getKLast4TotalStatsAsync()
        {
            int week = await _configurationDao.GetConfigurationValue("currentWeek");
            List<PlayerStatsExtDto> kLast4TotalStats = new List<PlayerStatsExtDto>();
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand(SELECT_SQL + GROUP_BY_SQL, connection))
                {
                    command.Parameters.AddWithValue("@week", week - 1);
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            kLast4TotalStats.Add(MapRowToKStat(reader));
                        }
                    }
                }
            }
            return kLast4TotalStats;
        }

        public async Task<List<PlayerStatsExtDto>> getKLast4TotalStatsByConfAsync(string conf)
        {
            int week = await _configurationDao.GetConfigurationValue("currentWeek");
            List<PlayerStatsExtDto> kLast4TotalStatsByConf = new List<PlayerStatsExtDto>();
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand(SELECT_SQL + CONF_SQL + GROUP_BY_SQL, connection))
                {
                    command.Parameters.AddWithValue("@week", week - 1);
                    command.Parameters.AddWithValue("@conf", $"%{conf}%");
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            kLast4TotalStatsByConf.Add(MapRowToKStat(reader));
                        }
                    }
                }
            }
            return kLast4TotalStatsByConf;
        }

        public async Task<List<PlayerStatsExtDto>> getKLast4TotalStatsByTeamAsync(string team)
        {
            int week = await _configurationDao.GetConfigurationValue("currentWeek");
            List<PlayerStatsExtDto> kLast4TotalStatsByTeam = new List<PlayerStatsExtDto>();
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand(SELECT_SQL + TEAM_SQL + GROUP_BY_SQL, connection))
                {
                    command.Parameters.AddWithValue("@week", week - 1);
                    command.Parameters.AddWithValue("@team", $"%{team}%");
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            kLast4TotalStatsByTeam.Add(MapRowToKStat(reader));
                        }
                    }
                }
            }
            return kLast4TotalStatsByTeam;
        }

        public async Task<List<PlayerStatsExtDto>> getKLast4TotalStatsByNameAsync(string name)
        {
            int week = await _configurationDao.GetConfigurationValue("currentWeek");
            List<PlayerStatsExtDto> kLast4TotalStatsByName = new List<PlayerStatsExtDto>();
            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (NpgsqlCommand command = new NpgsqlCommand(SELECT_SQL + NAME_SQL + GROUP_BY_SQL, connection))
                {
                    command.Parameters.AddWithValue("@week", week - 1);
                    command.Parameters.AddWithValue("@name", $"%{name}%");
                    using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            kLast4TotalStatsByName.Add(MapRowToKStat(reader));
                        }
                    }
                }
            }
            return kLast4TotalStatsByName;
        }


        private PlayerStatsExtDto MapRowToKStat(NpgsqlDataReader reader)
        {
            return new PlayerStatsExtDto()
            {
                PlayerId = Convert.ToInt32(reader["player_id"]),
                Week = Convert.ToInt32(reader["week"]),
                Position = Convert.ToString(reader["position"]),
                Team = Convert.ToString(reader["team"]),
                Name = Convert.ToString(reader["name"]),
                Status = Convert.ToString(reader["status"]),
                InjuryStatus = Convert.ToString(reader["injury_status"]),
                FieldGoalsMade = Convert.ToDouble(reader["field_goals_made"]),
                FieldGoalsAttempted = Convert.ToDouble(reader["field_goals_attempted"]),
                FieldGoalPercentage = Convert.ToDouble(reader["field_goal_percentage"]),
                FieldGoalsMade0to19 = Convert.ToDouble(reader["field_goals_made_0_to_19"]),
                FieldGoalsMade20to29 = Convert.ToDouble(reader["field_goals_made_20_to_29"]),
                FieldGoalsMade30to39 = Convert.ToDouble(reader["field_goals_made_30_to_39"]),
                FieldGoalsMade40to49 = Convert.ToDouble(reader["field_goals_made_40_to_49"]),
                FieldGoalsMade50Plus = Convert.ToDouble(reader["field_goals_made_50_plus"]),
                ExtraPointsMade = Convert.ToDouble(reader["extra_points_made"]),
                ExtraPointsAttempted = Convert.ToDouble(reader["extra_points_attempted"]),
                ExtraPointPercentage = Convert.ToDouble(reader["extra_point_percentage"]),
                FantasyPointsTotal = Convert.ToDouble(reader["fantasy_points_total"]),
                FantasyPointsAverage = Convert.ToDouble(reader["fantasy_points_average"]),
                Conference = Convert.ToString(reader["conference"]),
                TeamStatus = Convert.ToString(reader["team_status"])
            };
        }
    }
}