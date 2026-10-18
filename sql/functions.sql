-- ПРОЦЕДУРЫ И ФУНКЦИИ
-- Регистрация нового прослушивания с автоматической фиксацией времени
CREATE OR REPLACE PROCEDURE log_listening(p_user_id INT, p_track_id INT, p_duration INT)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO listening_history (user_id, track_id, listened_duration)
    VALUES (p_user_id, p_track_id, p_duration);
END;
$$;

-- Добавление трека в плейлист с обновлением даты изменения плейлиста
CREATE OR REPLACE PROCEDURE add_track_to_playlist(p_track_id INT, p_playlist_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO tracks_of_playlist (track_id, playlist_id) VALUES (p_track_id, p_playlist_id);
    UPDATE playlists SET updated_at = NOW() WHERE playlist_id = p_playlist_id;
END;
$$;

-- Безопасная смена роли пользователя с записью в аудит
CREATE OR REPLACE PROCEDURE change_user_role(p_user_id INT, p_new_role_id INT, p_admin_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE users SET role_id = p_new_role_id WHERE user_id = p_user_id;
    INSERT INTO audit_log (user_id, operation, operation_status) 
    VALUES (p_admin_id, 'CHANGE_ROLE_OF_USER_' || p_user_id, 1);
END;
$$;

-- Функция расчета общего времени звучания плейлиста
CREATE OR REPLACE FUNCTION get_playlist_duration(p_playlist_id INT)
RETURNS INT LANGUAGE plpgsql AS $$
DECLARE
    v_total_duration INT;
BEGIN
    SELECT COALESCE(SUM(duration), 0) INTO v_total_duration
    FROM tracks t
    JOIN tracks_of_playlist top ON t.track_id = top.track_id
    WHERE top.playlist_id = p_playlist_id;
    RETURN v_total_duration;
END;
$$;

-- Функция проверки: является ли пользователь автором конкретного трека
CREATE OR REPLACE FUNCTION is_track_author(p_user_id INT, p_track_id INT)
RETURNS BOOLEAN LANGUAGE plpgsql AS $$
DECLARE
    v_is_author BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1 FROM authors_of_track aot
        JOIN authors a ON aot.author_id = a.author_id
        JOIN users u ON u.user_id = p_user_id
        WHERE aot.track_id = p_track_id AND a.author_name = u.username
    ) INTO v_is_author;
    RETURN v_is_author;
END;
$$;