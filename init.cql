CREATE KEYSPACE IF NOT EXISTS nosql_chat WITH REPLICATION = { 'class': 'SimpleStrategy', 'replication_factor': 2 };

USE nosql_chat;

CREATE TABLE IF NOT EXISTS users
(
    user_id    UUID,
    email      TEXT,
    username   TEXT,
    created_at TIMESTAMP,
    PRIMARY KEY ( (user_id) )
);

CREATE TABLE IF NOT EXISTS users_by_email
(
    email         TEXT,
    password_hash TEXT,
    user_id       UUID,
    PRIMARY KEY (email)
);

CREATE TABLE IF NOT EXISTS rooms
(
    room_id     UUID,
    name        TEXT,
    description TEXT,
    created_at  TIMESTAMP,
    PRIMARY KEY ( room_id )
);

CREATE TABLE IF NOT EXISTS rooms_by_user
(
    user_id          UUID,
    room_id          UUID,
    room_name        TEXT,
    room_description TEXT,
    PRIMARY KEY ((user_id), room_id)
) WITH CLUSTERING ORDER BY (room_id DESC);

CREATE TABLE IF NOT EXISTS users_by_room
(
    room_id           UUID,
    joined_at         TIMESTAMP,
    user_id           UUID,
    user_display_name TEXT,
    roles             SET<TEXT>,
    PRIMARY KEY ((room_id), joined_at, user_id)
) WITH CLUSTERING ORDER BY (joined_at ASC, user_id DESC);

CREATE TABLE IF NOT EXISTS messages_by_room
(
    room_id     UUID,
    message_id  TIMEUUID,
    sender_name TEXT,
    content     TEXT,
    is_pinned   BOOLEAN,
    PRIMARY KEY ((room_id), message_id)
) WITH CLUSTERING ORDER BY (message_id DESC) AND DEFAULT_TIME_TO_LIVE = 31536000; -- 1 year

CREATE INDEX IF NOT EXISTS ON messages_by_room (is_pinned);

CREATE TABLE IF NOT EXISTS messages_by_room_and_user
(
    room_id     UUID,
    user_id     UUID,
    message_id  TIMEUUID,
    sender_name TEXT,
    content     TEXT,
    PRIMARY KEY ((room_id, user_id), message_id)
) WITH CLUSTERING ORDER BY (message_id DESC) AND DEFAULT_TIME_TO_LIVE = 31536000; -- 1 year