CREATE INDEX IF NOT EXISTS idx_listening_history_tracks ON listening_history(track_id);

-- До и после создания индекса
EXPLAIN ANALYZE 
SELECT track_id, COUNT(history_id) 
FROM listening_history 
GROUP BY track_id;