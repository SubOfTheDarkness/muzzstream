-- СОЗДАНИЕ/ПЕРЕСОЗДАНИЕ ТАБЛИЦ И НЕМНОГО ИХ ЗАПОЛНЕНИЕ(ДЛЯ СПРАВОЧНИКОВ)
DROP TABLE IF EXISTS roles CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS sessions CASCADE;
DROP TABLE IF EXISTS email_confirmations CASCADE;
DROP TABLE IF EXISTS authors CASCADE;
DROP TABLE IF EXISTS group_roles CASCADE;
DROP TABLE IF EXISTS author_members CASCADE;
DROP TABLE IF EXISTS genres CASCADE;
DROP TABLE IF EXISTS albums CASCADE;
DROP TABLE IF EXISTS authors_of_album CASCADE;
DROP TABLE IF EXISTS genres_of_album CASCADE;
DROP TABLE IF EXISTS compositions CASCADE;
DROP TABLE IF EXISTS tracks CASCADE;
DROP TABLE IF EXISTS authors_of_composition CASCADE;
DROP TABLE IF EXISTS genres_of_composition CASCADE;
DROP TABLE IF EXISTS playlists CASCADE;
DROP TABLE IF EXISTS tracks_of_playlist CASCADE;
DROP TABLE IF EXISTS listening_history CASCADE;
DROP TABLE IF EXISTS status_codes CASCADE;
DROP TABLE IF EXISTS audit_log CASCADE;

CREATE TABLE roles (
	role_id SERIAL PRIMARY KEY,
	role_name TEXT UNIQUE NOT NULL
);
INSERT INTO roles (role_name) VALUES ('Admin'), ('Moderator'), ('User');

CREATE TABLE users (
	user_id SERIAL primary key,
	nickname VARCHAR(16) UNIQUE NOT NULL,
	username TEXT,
	email TEXT unique not null,
	password TEXT not null,
	is_verified BOOLEAN not null default FALSE,
	role_id INT NOT NULL DEFAULT 3,
	created_at TIMESTAMPTZ not null default NOW(),
	FOREIGN KEY (role_id) REFERENCES roles(role_id) ON DELETE RESTRICT,
	CONSTRAINT check_username_length CHECK (LENGTH(username) > 3)
);

CREATE TABLE sessions (
	token TEXT primary key,
	user_id INT not null,
	device_name TEXT not null,
	ip_address VARCHAR(45) not null,
	foreign key (user_id) references users(user_id) ON DELETE CASCADE
);

CREATE TABLE email_confirmations (
    email TEXT PRIMARY KEY REFERENCES users(email) ON DELETE CASCADE,
    code VARCHAR(6) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL
);


CREATE TABLE author_statuses (
	status_id SERIAL PRIMARY KEY,
	status_text TEXT UNIQUE NOT NULL
);
INSERT INTO author_statuses (status_text) VALUES ('Unverified'), ('Verified'), ('Syntetic'), ('Limited'), ('Banned');

CREATE TABLE authors (
    author_id SERIAL PRIMARY KEY,
    author_name TEXT UNIQUE NOT NULL,
    country VARCHAR(50),
    current_status INT NOT NULL DEFAULT 1,
	status_until TIMESTAMPTZ,
	next_status INT,
    created_by_user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
	FOREIGN KEY (current_status) REFERENCES author_statuses(status_id) ON DELETE RESTRICT,
	FOREIGN KEY (next_status) REFERENCES author_statuses(status_id) ON DELETE RESTRICT
);

CREATE TABLE group_roles (
    group_role_id SERIAL PRIMARY KEY,
    role_name TEXT UNIQUE NOT NULL
);
INSERT INTO group_roles (role_name) VALUES ('Owner'), ('Manager'), ('Member');

CREATE TABLE author_members (
    author_id INT NOT NULL,
    user_id INT NOT NULL,
    group_role_id INT NOT NULL DEFAULT 3,
    PRIMARY KEY (author_id, user_id),
    FOREIGN KEY (author_id) REFERENCES authors(author_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    FOREIGN KEY (group_role_id) REFERENCES group_roles(group_role_id) ON DELETE RESTRICT
);


CREATE TABLE genres (
	genre_id SERIAL PRIMARY KEY,
	genre_name TEXT UNIQUE NOT NULL
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

CREATE TABLE compositions (
    composition_id SERIAL PRIMARY KEY,
    composition_name TEXT NOT NULL,
    release_year SMALLINT NOT NULL
);

CREATE TABLE tracks (
    track_id SERIAL PRIMARY KEY,
    composition_id INT NOT NULL,
    album_id INT,
    filepath TEXT UNIQUE NOT NULL,
    duration INT NOT NULL,
    version_type TEXT NOT NULL DEFAULT 'Studio',
    FOREIGN KEY (composition_id) REFERENCES compositions(composition_id) ON DELETE CASCADE,
    FOREIGN KEY (album_id) REFERENCES albums(album_id) ON DELETE SET NULL
);

CREATE TABLE authors_of_composition (
	composition_id INT NOT NULL,
	author_id INT NOT NULL,
	PRIMARY KEY (composition_id, author_id),
	FOREIGN KEY (composition_id) REFERENCES compositions (composition_id) ON DELETE CASCADE,
	FOREIGN KEY (author_id) REFERENCES authors (author_id) ON DELETE CASCADE
);

CREATE TABLE genres_of_composition (
	composition_id INT NOT NULL,
	genre_id INT NOT NULL,
	PRIMARY KEY (composition_id, genre_id),
	FOREIGN KEY (composition_id) REFERENCES compositions (composition_id) ON DELETE CASCADE,
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
