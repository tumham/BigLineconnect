CREATE TABLE #OUTER (id int, guid1 uniqueidentifier);
CREATE TABLE #INNER (guid uniqueidentifier, val varchar(10));

DECLARE @g uniqueidentifier = NEWID();
INSERT INTO #OUTER VALUES (1, @g);
INSERT INTO #INNER VALUES (@g, 'BEYAZ');

SELECT ISNULL((SELECT val FROM #INNER WHERE guid = guid1), '') AS v FROM #OUTER;

DROP TABLE #OUTER;
DROP TABLE #INNER;