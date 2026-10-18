-- ПРЕДСТАВЛЕНИЯ
-- Полная информация о треках со всеми авторами и альбомами
CREATE OR REPLACE VIEW view_track_details AS
SELECT 
    t.track_id,
    t.duration,
    t.version_type,
    t.filepath,
    c.composition_id,
    c.composition_name,
    c.release_year AS composition_release_year,
    a.album_id,
    a.album_name,
    a.release_year AS album_release_year,
    STRING_AGG(DISTINCT auth.author_name, ', ') AS authors_list
FROM tracks t
JOIN compositions c ON t.composition_id = c.composition_id
LEFT JOIN albums a ON t.album_id = a.album_id
LEFT JOIN authors_of_composition aoc ON c.composition_id = aoc.composition_id
LEFT JOIN authors auth ON aoc.author_id = auth.author_id
GROUP BY 
    t.track_id, t.duration, t.version_type, t.filepath,
    c.composition_id, c.composition_name, c.release_year,
    a.album_id, a.album_name, a.release_year;


-- 100 самых популярных треков на основе истории прослушиваний
CREATE OR REPLACE VIEW view_popular_tracks AS
SELECT vtd.*, COUNT(lh.history_id) AS total_plays
FROM view_track_details vtd
JOIN listening_history lh ON vtd.track_id = lh.track_id
GROUP BY vtd.track_id, vtd.track_name, vtd.release_year, vtd.duration, vtd.album_name, vtd.authors_list
ORDER BY total_plays DESC
LIMIT 100;

-- Публичные плейлисты пользователей с количеством треков в них
CREATE OR REPLACE VIEW view_public_playlists AS
SELECT p.playlist_id, p.playlist_name, u.email AS creator_email, COUNT(top.track_id) AS tracks_count
FROM playlists p
JOIN users u ON p.user_id = u.user_id
LEFT JOIN tracks_of_playlist top ON p.playlist_id = top.playlist_id
WHERE p.is_public = TRUE
GROUP BY p.playlist_id, p.playlist_name, u.email;

-- Лог безопасности с расшифровкой статусов
CREATE OR REPLACE VIEW view_security_audit AS
SELECT al.log_id, u.email, al.operation, sc.status_text, al.operation_time
FROM audit_log al
LEFT JOIN users u ON al.user_id = u.user_id
JOIN status_codes sc ON al.operation_status = sc.status_id;

-- Статистика активности пользователей
CREATE OR REPLACE VIEW view_user_activity AS
SELECT u.user_id, u.email, COUNT(lh.history_id) AS total_tracks_listened,
       ROUND(SUM(lh.listened_duration) / 60.0, 2) AS total_minutes_listened
FROM users u
LEFT JOIN listening_history lh ON u.user_id = lh.user_id
GROUP BY u.user_id, u.email;