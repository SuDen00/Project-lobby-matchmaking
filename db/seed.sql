-- Тестовые данные для системы лобби
-- Допущение: schema.sql уже выполнен в пустой базе

-- Игроки
INSERT INTO players (display_name, platform_id) VALUES
    ('Denis',    'steam-111111111'),
    ('Alex',     'steam-222222222'),
    ('Sasha',    'steam-333333333');

-- Лобби
INSERT INTO lobbies (name, type, join_code, max_players, current_players, status) VALUES
    ('Public test lobby',   'public',  NULL,     4, 2, 'open'),
    ('Private by code',     'private', '123456', 4, 1, 'open'),
    ('Old closed lobby',    'public',  '654321', 4, 4, 'closed');

-- Участники
-- Здесь предполагается, что id лобби и игроков начинаются с 1
INSERT INTO members (lobby_id, player_id) VALUES
    (1, 1),  -- Denis в Public test lobby
    (1, 2),  -- Alex в Public test lobby
    (2, 3);  -- Sasha в Private by code
