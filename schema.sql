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
INSERT INTO roles (role_name) VALUES ('Admin'), ('User');

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
	email TEXT unique not null,
	password TEXT not null,
	is_verified BOOLEAN not null default FALSE,
	role_id INT NOT NULL DEFAULT 2,
	created_at TIMESTAMPTZ not null default NOW(),
	FOREIGN KEY (role_id) REFERENCES roles(role_id) ON DELETE RESTRICT
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


