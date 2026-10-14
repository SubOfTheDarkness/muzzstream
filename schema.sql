-- СОЗДАНИЕ/ПЕРЕСОЗДАНИЕ ТАБЛИЦ И НЕМНОГО ИХ ЗАПОЛНЕНИЕ(ДЛЯ СПРАВОЧНИКОВ)
DROP TABLE IF EXISTS roles;
DROP TABLE IF EXISTS authors;
DROP TABLE IF EXISTS genres;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS sessions;
DROP TABLE IF EXISTS albums;
DROP TABLE IF EXISTS authors_of_album;
DROP TABLE IF EXISTS genres_of_album;
DROP TABLE IF EXISTS tracks;
DROP TABLE IF EXISTS authors_of_track;
DROP TABLE IF EXISTS genres_of_track;
DROP TABLE IF EXISTS playlists;
DROP TABLE IF EXISTS tracks_of_playlist;
DROP TABLE IF EXISTS listening_history;
DROP TABLE IF EXISTS status_codes;
DROP TABLE IF EXISTS audit_log;

CREATE TABLE roles (
	role_id SERIAL PRIMARY KEY,
	role_name TEXT UNIQUE NOT NULL
);
INSERT INTO roles (role_name) VALUES ('Admin'), ('Moderator'), ('Artist'), ('Listener');

CREATE TABLE authors (
	author_id SERIAL PRIMARY KEY,
	author_name TEXT NOT NULL,
	country VARCHAR(50)
);

CREATE TABLE genres (
	genre_id SERIAL PRIMARY KEY,
	genre_name TEXT UNIQUE NOT NULL
);

CREATE TABLE users (
	user_id SERIAL primary key,
	nickname VARCHAR(16) UNIQUE NOT NULL,
	username TEXT,
	email TEXT unique not null,
	password TEXT not null,
	is_verified BOOLEAN not null default FALSE,
	role_id INT NOT NULL DEFAULT 4,
	created_at TIMESTAMPTZ not null default NOW(),
	FOREIGN KEY (role_id) REFERENCES roles(role_id) ON DELETE RESTRICT,
	CONSTRAINT check_username_length CHECK (LENGTH(username) > 3)
);

CREATE TABLE sessions (
	token TEXT primary key,
	user_id INT not null,
	device_name TEXT not null,
	ip_address VARCHAR(15) not null,
	foreign key (user_id) references users(user_id) ON DELETE CASCADE
);

CREATE TABLE albums (
	album_id SERIAL PRIMARY KEY,
	album_name TEXT NOT NULL,
	release_year SMALLINT NOT NULL
);

CREATE TABLE authors_of_album (
	album_id INT NOT NULL,
	author_id INT NOT NULL,
	PRIMARY KEY (album_id, author_id),
	FOREIGN KEY (album_id) REFERENCES albums (album_id) ON DELETE CASCADE,
	FOREIGN KEY (author_id) REFERENCES authors (author_id) ON DELETE CASCADE
);

CREATE TABLE genres_of_album (
	album_id INT NOT NULL,
	genre_id INT NOT NULL,
	PRIMARY KEY (album_id, genre_id),
	FOREIGN KEY (album_id) REFERENCES albums (album_id) ON DELETE CASCADE,
	FOREIGN KEY (genre_id) REFERENCES genres (genre_id) ON DELETE RESTRICT
);

CREATE TABLE tracks (
	track_id SERIAL PRIMARY KEY,
	track_name TEXT NOT NULL,
	release_year SMALLINT NOT NULL,
	album_id INT,
	duration INT NOT NULL,
	filepath TEXT UNIQUE NOT NULL,
	FOREIGN KEY (album_id) REFERENCES albums (album_id) ON DELETE SET NULL
);

CREATE TABLE authors_of_track (
	track_id INT NOT NULL,
	author_id INT NOT NULL,
	PRIMARY KEY (track_id, author_id),
	FOREIGN KEY (track_id) REFERENCES tracks (track_id) ON DELETE CASCADE,
	FOREIGN KEY (author_id) REFERENCES authors (author_id) ON DELETE CASCADE
);

CREATE TABLE genres_of_track (
	track_id INT NOT NULL,
	genre_id INT NOT NULL,
	PRIMARY KEY (track_id, genre_id),
	FOREIGN KEY (track_id) REFERENCES tracks (track_id) ON DELETE CASCADE,
	FOREIGN KEY (genre_id) REFERENCES genres (genre_id) ON DELETE RESTRICT
);

CREATE TABLE playlists (
	playlist_id SERIAL PRIMARY KEY,
	user_id INT NOT NULL,
	playlist_name TEXT NOT NULL,
	is_public BOOLEAN NOT NULL DEFAULT FALSE,
	created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
);

CREATE TABLE tracks_of_playlist (
	track_id INT NOT NULL,
	playlist_id INT NOT NULL,
	PRIMARY KEY (track_id, playlist_id),
	FOREIGN KEY (track_id) REFERENCES tracks (track_id) ON DELETE CASCADE,
	FOREIGN KEY (playlist_id) REFERENCES playlists (playlist_id) ON DELETE CASCADE
);

CREATE TABLE listening_history (
	history_id SERIAL PRIMARY KEY,
	user_id INT NOT NULL,
	track_id INT NOT NULL,
	listened_duration INT NOT NULL,
	listening_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	FOREIGN KEY (track_id) REFERENCES tracks (track_id) ON DELETE CASCADE,
	FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE
);

CREATE TABLE status_codes (
	status_id SERIAL PRIMARY KEY,
	status_text TEXT UNIQUE NOT NULL
);
INSERT INTO status_codes (status_text) VALUES ('SUCCESS'), ('FAILED'), ('ACCESS_DENIED');

CREATE TABLE audit_log (
	log_id SERIAL PRIMARY KEY,
	user_id INT,
	operation TEXT NOT NULL,
	operation_status INT NOT NULL DEFAULT 1,
	operation_time TIMESTAMPTZ NOT NULL DEFAULT NOW(),
	FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE SET NULL,
	FOREIGN KEY (operation_status) REFERENCES status_codes (status_id) ON DELETE RESTRICT
);


-- ПРЕДСТАВЛЕНИЯ
-- Полная информация о треках со всеми авторами и альбомами
CREATE OR REPLACE VIEW view_track_details AS
SELECT 
    t.track_id, t.track_name, t.release_year, t.duration,
    a.album_name,
    STRING_AGG(auth.author_name, ', ') AS authors_list
FROM tracks t
LEFT JOIN albums a ON t.album_id = a.album_id
LEFT JOIN authors_of_track aot ON t.track_id = aot.track_id
LEFT JOIN authors auth ON aot.author_id = auth.author_id
GROUP BY t.track_id, t.track_name, t.release_year, t.duration, a.album_name;

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


-- ТРИГГЕРЫ
-- Автоматическое логирование критических событий (Аудит)
CREATE OR REPLACE FUNCTION trigger_audit_user_changes()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    IF (TG_OP = 'INSERT') THEN
        INSERT INTO audit_log (user_id, operation, operation_status) VALUES (NEW.user_id, 'USER_REGISTERED', 1);
    ELSIF (TG_OP = 'UPDATE') THEN
        IF OLD.password <> NEW.password THEN
            INSERT INTO audit_log (user_id, operation, operation_status) VALUES (NEW.user_id, 'PASSWORD_CHANGED', 1);
        END IF;
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_user_audit
AFTER INSERT OR UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION trigger_audit_user_changes();


-- Защита системных логов от удаления
CREATE OR REPLACE FUNCTION trigger_prevent_log_deletion()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'Удаление системных логов безопасности запрещено архитектурой.';
    RETURN NULL;
END;
$$;

CREATE TRIGGER trg_protect_audit_logs
BEFORE DELETE OR UPDATE ON audit_log
FOR EACH ROW EXECUTE FUNCTION trigger_prevent_log_deletion();
