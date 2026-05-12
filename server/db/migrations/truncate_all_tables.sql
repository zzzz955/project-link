-- Truncate all tables to reset data (run after applying schema changes)
-- WARNING: This deletes all data. Run only on dev/test environments or after confirming with team.
-- Order matters: disable FK checks first in MySQL

SET FOREIGN_KEY_CHECKS = 0;

TRUNCATE TABLE sessions;
TRUNCATE TABLE stage_progress;
TRUNCATE TABLE stage_best_records;
TRUNCATE TABLE user_ranking_cache;
TRUNCATE TABLE user_currency;
TRUNCATE TABLE user_profiles;
TRUNCATE TABLE stamina_state;
TRUNCATE TABLE inventory;
TRUNCATE TABLE currency_logs;
TRUNCATE TABLE daily_challenge_progress;
TRUNCATE TABLE player_settings;
TRUNCATE TABLE action_logs;
TRUNCATE TABLE client_meta;

SET FOREIGN_KEY_CHECKS = 1;
