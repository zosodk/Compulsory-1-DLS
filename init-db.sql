-- defaultdb
SELECT 'CREATE DATABASE defaultdb'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'defaultdb')\gexec;

-- shard1db
SELECT 'CREATE DATABASE shard1db'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'shard1db')\gexec;

-- shard2db
SELECT 'CREATE DATABASE shard2db'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'shard2db')\gexec;


\c defaultdb;

CREATE TABLE IF NOT EXISTS FileEntity (
                                          FileId SERIAL PRIMARY KEY,
                                          FileName VARCHAR(255) NOT NULL,
    Content BYTEA NOT NULL
    );

CREATE TABLE IF NOT EXISTS Word (
                                    WordId SERIAL PRIMARY KEY,
                                    WordText VARCHAR(255) NOT NULL UNIQUE
    );

CREATE TABLE IF NOT EXISTS Occurrence (
                                          WordId INT NOT NULL,
                                          FileId INT NOT NULL,
                                          Count INT NOT NULL,
                                          PRIMARY KEY (WordId, FileId),
    FOREIGN KEY (WordId) REFERENCES Word(WordId) ON DELETE CASCADE,
    FOREIGN KEY (FileId) REFERENCES FileEntity(FileId) ON DELETE CASCADE
    );


\c shard1db;

CREATE TABLE IF NOT EXISTS FileEntity (
                                          FileId SERIAL PRIMARY KEY,
                                          FileName VARCHAR(255) NOT NULL,
    Content BYTEA NOT NULL
    );

CREATE TABLE IF NOT EXISTS Word (
                                    WordId SERIAL PRIMARY KEY,
                                    WordText VARCHAR(255) NOT NULL UNIQUE
    );

CREATE TABLE IF NOT EXISTS Occurrence (
                                          WordId INT NOT NULL,
                                          FileId INT NOT NULL,
                                          Count INT NOT NULL,
                                          PRIMARY KEY (WordId, FileId),
    FOREIGN KEY (WordId) REFERENCES Word(WordId) ON DELETE CASCADE,
    FOREIGN KEY (FileId) REFERENCES FileEntity(FileId) ON DELETE CASCADE
    );


\c shard2db;

CREATE TABLE IF NOT EXISTS FileEntity (
                                          FileId SERIAL PRIMARY KEY,
                                          FileName VARCHAR(255) NOT NULL,
    Content BYTEA NOT NULL
    );

CREATE TABLE IF NOT EXISTS Word (
                                    WordId SERIAL PRIMARY KEY,
                                    WordText VARCHAR(255) NOT NULL UNIQUE
    );

CREATE TABLE IF NOT EXISTS Occurrence (
                                          WordId INT NOT NULL,
                                          FileId INT NOT NULL,
                                          Count INT NOT NULL,
                                          PRIMARY KEY (WordId, FileId),
    FOREIGN KEY (WordId) REFERENCES Word(WordId) ON DELETE CASCADE,
    FOREIGN KEY (FileId) REFERENCES FileEntity(FileId) ON DELETE CASCADE
    );
