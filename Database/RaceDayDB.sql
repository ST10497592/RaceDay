-- RaceDay Database Script
-- This file will contain tables, constraints, seed data, and verification queries.


CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    PhoneNumber VARCHAR(20) NULL,
    DateOfBirth DATE NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Organiser','Participant'))
);
GO

CREATE TABLE Events (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    EventName VARCHAR(150) NOT NULL,
    Description VARCHAR(500) NOT NULL,
    EventType VARCHAR(50) NOT NULL,
    EventDate DATE NOT NULL,
    StartTime TIME NOT NULL,
    Location VARCHAR(200) NOT NULL,
    EntryFee DECIMAL(10,2) NOT NULL,
    MaxParticipants INT NOT NULL,
    Status VARCHAR(20) NOT NULL,
    BannerUrl VARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Organiser FOREIGN KEY (OrganiserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Events_Type CHECK (EventType IN ('Running','Walking','Cycling')),
    CONSTRAINT CK_Events_Fee CHECK (EntryFee >= 0),
    CONSTRAINT CK_Events_MaxParticipants CHECK (MaxParticipants > 0),
    CONSTRAINT CK_Events_Status CHECK (Status IN ('Upcoming','Open','Closed','Completed','Cancelled'))
);
GO

CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(255) NULL,
    MinimumAge INT NULL,
    MaximumAge INT NULL,
    CONSTRAINT CK_Categories_MinAge CHECK (MinimumAge IS NULL OR MinimumAge >= 0),
    CONSTRAINT CK_Categories_MaxAge CHECK (MaximumAge IS NULL OR MaximumAge >= 0),
    CONSTRAINT CK_Categories_AgeRange CHECK (
        MinimumAge IS NULL OR MaximumAge IS NULL OR MinimumAge <= MaximumAge
    )
);
GO

CREATE TABLE EventCategories (
    EventId INT NOT NULL,
    CategoryId INT NOT NULL,
    CONSTRAINT PK_EventCategories PRIMARY KEY (EventId, CategoryId),
    CONSTRAINT FK_EventCategories_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_EventCategories_Category FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);
GO

CREATE TABLE Enrolments (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    ParticipantId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL,
    PaymentStatus VARCHAR(20) NOT NULL,
    CONSTRAINT FK_Enrolments_Event FOREIGN KEY (EventId) REFERENCES Events(EventId),
    CONSTRAINT FK_Enrolments_Participant FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Category FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT CK_Enrolments_Status CHECK (
        Status IN ('Pending','Confirmed','Cancelled','Completed')
    ),
    CONSTRAINT CK_Enrolments_PaymentStatus CHECK (
        PaymentStatus IN ('Pending','Paid','Failed','Refunded')
    ),
    CONSTRAINT UQ_Enrolment_Participant_Event UNIQUE (ParticipantId, EventId)
);
GO

CREATE TABLE Results (
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    Position INT NULL,
    ResultStatus VARCHAR(20) NOT NULL,
    RecordedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolment FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId),
    CONSTRAINT CK_Results_Position CHECK (Position IS NULL OR Position > 0),
    CONSTRAINT CK_Results_Status CHECK (
        ResultStatus IN ('Finished','DidNotFinish','Disqualified')
    )
);
GO

-- Seed data: 2 organisers, 2 participants
INSERT INTO Users
    (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber, DateOfBirth)
VALUES
    ('James', 'Mokoena', 'james.mokoena@raceday.test', 'HASHED_PASSWORD_001', 'Organiser', '0710000001', '1988-04-12'),
    ('Sarah', 'Naidoo', 'sarah.naidoo@raceday.test', 'HASHED_PASSWORD_002', 'Organiser', '0710000002', '1990-09-21'),
    ('John', 'Smith', 'john.smith@raceday.test', 'HASHED_PASSWORD_003', 'Participant', '0710000003', '1998-02-15'),
    ('Emily', 'Williams', 'emily.williams@raceday.test', 'HASHED_PASSWORD_004', 'Participant', '0710000004', '2001-07-08');
GO

INSERT INTO Categories
    (CategoryName, Description, MinimumAge, MaximumAge)
VALUES
    ('5K Run', 'Short-distance road running category.', 13, NULL),
    ('10K Run', 'Ten-kilometre road running category.', 16, NULL),
    ('Half Marathon', '21.1-kilometre running category.', 18, NULL),
    ('Cycling', 'Road cycling race category.', 16, NULL),
    ('Family Walk', 'Accessible recreational walking category.', 8, NULL);
GO

INSERT INTO Events
    (OrganiserId, EventName, Description, EventType, EventDate, StartTime, Location,
     EntryFee, MaxParticipants, Status, BannerUrl)
VALUES
    (1, 'Johannesburg 10K', 'Annual 10-kilometre city road race.',
     'Running', '2026-10-18', '07:00:00', 'Johannesburg CBD',
     180.00, 1000, 'Open', NULL),
    (2, 'Pretoria Family Walk', 'Community walking event for families.',
     'Walking', '2026-11-08', '08:00:00', 'Pretoria National Botanical Garden',
     80.00, 500, 'Open', NULL),
    (1, 'Gauteng Cycling Challenge', 'Competitive road cycling challenge.',
     'Cycling', '2026-11-22', '06:30:00', 'Midrand',
     250.00, 600, 'Upcoming', NULL);
GO

INSERT INTO EventCategories (EventId, CategoryId)
VALUES
    (1, 1),
    (1, 2),
    (2, 5),
    (3, 4);
GO

INSERT INTO Enrolments
    (EventId, ParticipantId, CategoryId, Status, PaymentStatus)
VALUES
    (1, 3, 2, 'Confirmed', 'Paid'),
    (1, 4, 2, 'Confirmed', 'Paid'),
    (2, 3, 5, 'Confirmed', 'Paid'),
    (3, 4, 4, 'Pending', 'Pending');
GO

INSERT INTO Results
    (EnrolmentId, FinishTime, Position, ResultStatus)
VALUES
    (1, '00:48:32', 17, 'Finished'),
    (2, '00:52:14', 29, 'Finished');
GO

-- Verification queries
SELECT * FROM Users;
SELECT * FROM Events;
SELECT * FROM Categories;
SELECT * FROM EventCategories;
SELECT * FROM Enrolments;
SELECT * FROM Results;

SELECT
    e.EventId,
    e.EventName,
    u.FirstName + ' ' + u.LastName AS Organiser
FROM Events e
INNER JOIN Users u ON e.OrganiserId = u.UserId;

SELECT
    en.EnrolmentId,
    e.EventName,
    u.FirstName + ' ' + u.LastName AS Participant,
    c.CategoryName,
    en.Status,
    en.PaymentStatus
FROM Enrolments en
INNER JOIN Events e ON en.EventId = e.EventId
INNER JOIN Users u ON en.ParticipantId = u.UserId
INNER JOIN Categories c ON en.CategoryId = c.CategoryId;

SELECT
    r.ResultId,
    e.EventName,
    u.FirstName + ' ' + u.LastName AS Participant,
    r.FinishTime,
    r.Position,
    r.ResultStatus
FROM Results r
INNER JOIN Enrolments en ON r.EnrolmentId = en.EnrolmentId
INNER JOIN Events e ON en.EventId = e.EventId
INNER JOIN Users u ON en.ParticipantId = u.UserId;
GO
