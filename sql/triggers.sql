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