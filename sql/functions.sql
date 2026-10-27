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

-- Безопасная смена роли пользователя
CREATE OR REPLACE PROCEDURE change_user_role(p_user_id INT, p_new_role_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE users SET role_id = p_new_role_id WHERE user_id = p_user_id;
    DELETE FROM sessions WHERE user_id = p_user_id;
END;
$$;

-- Каскадный снос пользователя
CREATE OR REPLACE PROCEDURE admin_delete_user_cascade(p_user_id INT)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM sessions WHERE user_id = p_user_id;
    DELETE FROM users WHERE user_id = p_user_id;
END;
$$;

-- Скрытие плейлиста
CREATE OR REPLACE PROCEDURE moderate_playlist_visibility(p_playlist_id INT, p_is_public BOOLEAN)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE playlists 
    SET is_public = p_is_public, 
        updated_at = NOW() 
    WHERE playlist_id = p_playlist_id;
END;
$$;