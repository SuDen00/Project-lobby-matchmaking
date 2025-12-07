-- Схема БД для системы лобби и матчмейкинга
-- Postgresql

-- На всякий случай чистим старые объекты
DROP TABLE IF EXISTS members CASCADE;
DROP TABLE IF EXISTS lobbies CASCADE;
DROP TABLE IF EXISTS players CASCADE;

DROP TYPE IF EXISTS lobby_status;
DROP TYPE IF EXISTS lobby_type;

-- Типы перечислений
CREATE TYPE lobby_type AS ENUM ('public', 'private');

CREATE TYPE lobby_status AS ENUM ('open', 'in_game', 'closed');

-- Таблица игроков
CREATE TABLE players (
    id           serial       PRIMARY KEY,
    display_name varchar(64)  NOT NULL,
    platform_id  varchar(32)  NOT NULL UNIQUE
);

-- Таблица лобби
CREATE TABLE lobbies (
    id              serial        PRIMARY KEY,
    name            varchar(64)   NOT NULL,
    type            lobby_type    NOT NULL DEFAULT 'public',
    join_code       varchar(16),
    max_players     integer       NOT NULL CHECK (max_players > 0 AND max_players <= 16),
    current_players integer       NOT NULL DEFAULT 0 CHECK (current_players >= 0 AND current_players <= max_players),
    status          lobby_status  NOT NULL DEFAULT 'open',
    created_at      timestamptz   NOT NULL DEFAULT now(),

    CONSTRAINT uq_lobby_join_code UNIQUE (join_code)
);

-- Связка "игрок в лобби"
CREATE TABLE members (
    lobby_id  integer     NOT NULL REFERENCES lobbies(id) ON DELETE CASCADE,
    player_id integer     NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    joined_at timestamptz NOT NULL DEFAULT now(),

    PRIMARY KEY (lobby_id, player_id)
);

-- Индексы для типичных запросов
CREATE INDEX idx_lobbies_status ON lobbies(status);
CREATE INDEX idx_lobbies_type_status ON lobbies(type, status);
