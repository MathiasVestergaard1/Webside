USE webapp;

-- Run once on existing databases. Old favorites keep an unknown (NULL) date.
ALTER TABLE favorites ADD COLUMN created_at TIMESTAMP NULL DEFAULT NULL;
ALTER TABLE favorites MODIFY COLUMN created_at TIMESTAMP NULL DEFAULT CURRENT_TIMESTAMP;
