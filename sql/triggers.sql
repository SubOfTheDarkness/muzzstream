-- ТРИГГЕРЫ

CREATE OR REPLACE FUNCTION trigger_audit_moderation_actions()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
DECLARE
    v_context_user_id INT;
BEGIN
    BEGIN
        v_context_user_id := NULLIF(CURRENT_SETTING('app.current_user_id', TRUE), '')::INT;
    EXCEPTION WHEN OTHERS THEN
        v_context_user_id := NULL;
    END;

    -- Изменение публичности плейлиста модерацией
    IF (TG_TABLE_NAME = 'playlists' AND TG_OP = 'UPDATE') THEN
        IF OLD.is_public <> NEW.is_public THEN
            INSERT INTO audit_log (user_id, operation, operation_status)
            VALUES (
                v_context_user_id, 
                'MODERATOR_CHANGED_PLAYLIST_ID_' || NEW.playlist_id || '_PUBLIC_TO_' || NEW.is_public::TEXT, 
                1
            );
        END IF;
        RETURN NEW;

    -- Удаление трека
    ELSIF (TG_TABLE_NAME = 'tracks' AND TG_OP = 'DELETE') THEN
        INSERT INTO audit_log (user_id, operation, operation_status)
        VALUES (
            v_context_user_id, 
            'MODERATOR_DELETED_TRACK_ID_' || OLD.track_id || '_FILEPATH_' || OLD.filepath, 
            1
        );
        RETURN OLD;

    -- Изменение роли пользователя Администратором
    ELSIF (TG_TABLE_NAME = 'users' AND TG_OP = 'UPDATE') THEN
        IF OLD.role_id <> NEW.role_id THEN
            INSERT INTO audit_log (user_id, operation, operation_status)
            VALUES (
                v_context_user_id, 
                'ADMIN_CHANGED_USER_ID_' || NEW.user_id || '_ROLE_FROM_' || OLD.role_id || '_TO_' || NEW.role_id, 
                1
            );
        END IF;
        RETURN NEW;
    END IF;

    RETURN NULL;
END;
$$;


DROP TRIGGER IF EXISTS trg_audit_playlist_visibility ON playlists;
CREATE TRIGGER trg_audit_playlist_visibility
AFTER UPDATE ON playlists
FOR EACH ROW EXECUTE FUNCTION trigger_audit_moderation_actions();

DROP TRIGGER IF EXISTS trg_audit_track_removal ON tracks;
CREATE TRIGGER trg_audit_track_removal
BEFORE DELETE ON tracks
FOR EACH ROW EXECUTE FUNCTION trigger_audit_moderation_actions();

DROP TRIGGER IF EXISTS trg_audit_user_role_shift ON users;
CREATE TRIGGER trg_audit_user_role_shift
AFTER UPDATE ON users
FOR EACH ROW EXECUTE FUNCTION trigger_audit_moderation_actions();