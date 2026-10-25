-- =====================================================================
-- CourtGo: Fix Vietnamese UTF-8 / Mojibake encoding for existing data
-- Note: Always prefix string literals with N'...' in T-SQL to preserve Unicode.
-- =====================================================================

UPDATE SportCenters
SET AddressLine = N'32 Huỳnh Tấn Phát',
    Ward = N'Tân Thuận Đông',
    District = N'Quận 7',
    City = N'Hồ Chí Minh'
WHERE Id = '11111111-1111-1111-1111-111111111111';

UPDATE Bookings
SET CustomerNameSnapshot = N'Nguyễn Văn A'
WHERE CustomerNameSnapshot LIKE '%Nguy%n%';

UPDATE Bookings
SET CourtNameSnapshot = N'Sân A1'
WHERE CourtNameSnapshot LIKE '%S%n A1%';

UPDATE Bookings
SET SportNameSnapshot = N'Cầu lông'
WHERE SportNameSnapshot LIKE '%C%u l%ng%';

UPDATE CourtBlocks
SET Reason = N'Bảo trì định kỳ'
WHERE Reason LIKE '%B%o tr%';
GO
