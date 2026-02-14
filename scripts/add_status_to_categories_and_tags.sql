-- Add CategoryStatus to Category table
IF COL_LENGTH('Category','CategoryStatus') IS NULL
BEGIN
    ALTER TABLE Category ADD CategoryStatus int NOT NULL CONSTRAINT DF_Category_CategoryStatus DEFAULT(1);
    -- Update existing records to Approved (3) if IsActive = true, otherwise Draft (1)
    UPDATE Category SET CategoryStatus = CASE WHEN IsActive = 1 THEN 3 ELSE 1 END;
END
GO

-- Add TagStatus and IsActive to Tag table
IF COL_LENGTH('Tag','TagStatus') IS NULL
BEGIN
    ALTER TABLE Tag ADD TagStatus int NOT NULL CONSTRAINT DF_Tag_TagStatus DEFAULT(1);
    -- Update existing records to Approved (3)
    UPDATE Tag SET TagStatus = 3 WHERE IsDeleted = 0;
END
GO

IF COL_LENGTH('Tag','IsActive') IS NULL
BEGIN
    ALTER TABLE Tag ADD IsActive bit NULL;
    -- Set IsActive = true for existing non-deleted tags
    UPDATE Tag SET IsActive = 1 WHERE IsDeleted = 0;
END
GO

