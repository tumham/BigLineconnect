SELECT TOP 10 h.sth_stok_kod, b.* 
FROM STOK_HAREKETLERI h 
INNER JOIN BEDEN_HAREKETLERI b ON h.sth_Guid = b.BdnHar_Har_uid 
WHERE h.sth_stok_kod = 'BXR99998'
