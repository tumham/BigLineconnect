import sys

def modify_sql():
    path = r'C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\SIPARIS_KARSILAMA_V16\SQL\SIPARIS_KARSILAMA_V16.sql'
    with open(path, 'r', encoding='cp1254') as f:
        content = f.read()

    # A_sp_SK_BedenHar_Kaydet modification
    old_sig = '''@Miktar as float,		--Teslim Miktarý
@BedenNo as integer,		--Beden No
@sth_satirno as integer)	--Satýr No'''
    new_sig = '''@Miktar as float,		--Teslim Miktarý
@BedenNo as integer,		--Beden No
@sth_satirno as integer,	--Satýr No
@VarGuid1 as uniqueidentifier = NULL)	--VarGuid'''

    if old_sig in content:
        content = content.replace(old_sig, new_sig)
    else:
        print("Sig 1 not found")

    old_insert = '''INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik)
VALUES(0, 0, 0, 113, 0, 0, 0, 0, @sth_create_user, getdate(), 
@sth_create_user, getdate(), '','','',11, @rn, 
@BedenNo,@Miktar, 0, 0, 0)--*'''

    new_insert = '''INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_VaryantPNTR, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik, BdnHar_Guid)
VALUES(0, 0, 0, 113, 0, 0, 0, 0, @sth_create_user, getdate(), 
@sth_create_user, getdate(), '','','',11, @rn, 
ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)),@Miktar, 0, 0, 0, NEWID())--*'''

    if old_insert in content:
        content = content.replace(old_insert, new_insert)
    else:
        print("Insert 1 not found")

    old_update1 = '''	UPDATE dbo.BEDEN_HAREKETLERI
	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar
	WHERE (BdnHar_Tipi=9 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_BedenNo=@BedenNo)'''
    new_update1 = '''	UPDATE dbo.BEDEN_HAREKETLERI
	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar
	WHERE (BdnHar_Tipi=9 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_VaryantPNTR=ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)))'''
    if old_update1 in content:
        content = content.replace(old_update1, new_update1)

    old_update2 = '''	UPDATE dbo.BEDEN_HAREKETLERI
	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar
	WHERE (BdnHar_Tipi=1 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_BedenNo=@BedenNo)'''
    new_update2 = '''	UPDATE dbo.BEDEN_HAREKETLERI
	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar
	WHERE (BdnHar_Tipi=1 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_VaryantPNTR=ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)))'''
    if old_update2 in content:
        content = content.replace(old_update2, new_update2)


    # A_sp_SK_BedenHar_Kaydet_2 modification
    old_sig_2 = '''@Miktar as float,		--Teslim Miktarý
@BedenNo as integer)		--Beden No'''
    new_sig_2 = '''@Miktar as float,		--Teslim Miktarý
@BedenNo as integer,		--Beden No
@VarGuid1 as uniqueidentifier = NULL)	--VarGuid'''

    if old_sig_2 in content:
        content = content.replace(old_sig_2, new_sig_2)
    else:
        print("Sig 2 not found")

    old_insert_2 = '''INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik)
VALUES(0, 0, 0, 113, 0, 0, 0, 0, @sth_create_user, getdate(), 
@sth_create_user, getdate(), '','','',11, @rn, 
@BedenNo,@Miktar, 0, 0, 0)--*'''

    if old_insert_2 in content:
        content = content.replace(old_insert_2, new_insert)
    else:
        print("Insert 2 not found")


    with open(path, 'w', encoding='cp1254') as f:
        f.write(content)
    print("SQL Update Complete!")

modify_sql()