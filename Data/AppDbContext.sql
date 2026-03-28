CREATE DATABASE TimePlannerDB;
USE TimePlannerDB;

CREATE TABLE Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,

    INDEX idx_user_email (Email)
);

CREATE TABLE Categories (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    ColorHex VARCHAR(7) NOT NULL,

    CONSTRAINT chk_color_format
        CHECK (ColorHex REGEXP '^#[0-9A-Fa-f]{6}$')
);

CREATE TABLE Events (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT NOT NULL,
    Title VARCHAR(100) NOT NULL,
    Description TEXT,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    CategoryId INT,
    IsRecurring BOOLEAN DEFAULT FALSE,
    RepeatType ENUM('none','daily','weekly','monthly') DEFAULT 'none',
    IsDeleted BOOLEAN DEFAULT FALSE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT chk_event_time
        CHECK (EndTime > StartTime),

    CONSTRAINT fk_event_user
        FOREIGN KEY (UserId) REFERENCES Users(Id)
        ON DELETE CASCADE,

    CONSTRAINT fk_event_category
        FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
        ON DELETE SET NULL,
        
    INDEX idx_event_user (UserId),
    INDEX idx_event_time (StartTime, EndTime)
);

CREATE TABLE Reminders (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    EventId INT NOT NULL,

    RemindAt DATETIME NOT NULL,
    IsSent BOOLEAN DEFAULT FALSE,

    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_reminder_event
        FOREIGN KEY (EventId) REFERENCES Events(Id)
        ON DELETE CASCADE,

    INDEX idx_reminder_time (RemindAt)
);