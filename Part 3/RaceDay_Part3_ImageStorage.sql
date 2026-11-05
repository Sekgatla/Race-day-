/*
  RaceDay Part 3 schema extension.
  Apply after the Part 2 EF Core migrations to persist image storage keys.
  The application migration is the source of truth for automated upgrades.
*/
ALTER TABLE dbo.Events
    ADD BannerImageKey nvarchar(500) NULL;
GO

ALTER TABLE dbo.Users
    ADD ProfileImageKey nvarchar(500) NULL;
GO
