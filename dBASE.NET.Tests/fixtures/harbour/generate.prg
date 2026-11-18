REQUEST DBFCDX, DBFFPT, HB_CODEPAGE_UA866, HB_CODEPAGE_UTF8

PROCEDURE Main( cDir, cMode, cRows )
   LOCAL cName, oError, nRows
   LOCAL aNames := { "basic", "special", "numeric", "wide", "empty", "single", "test_main" }

   IF Empty( cDir ) .OR. ! hb_DirExists( cDir )
      OutErr( "Expected an existing output directory" + hb_eol() )
      ErrorLevel( 2 )
      RETURN
   ENDIF
   cDir := hb_DirSepAdd( cDir )
   IF cMode == "benchmark"
      nRows := Val( cRows )
      IF nRows < 1 .OR. nRows > 1000000 .OR. cRows != hb_ntos( nRows ) .OR. nRows != Int( nRows )
         OutErr( "Expected an integer row count from 1 to 1000000" + hb_eol() )
         ErrorLevel( 2 )
         RETURN
      ENDIF
      aNames := { "test_main" }
   ELSEIF ! Empty( cMode )
      OutErr( "Usage: generate <directory> [benchmark <rows>]" + hb_eol() )
      ErrorLevel( 2 )
      RETURN
   ENDIF

   FOR EACH cName IN aNames
      IF hb_FileExists( cDir + cName + ".dbf" ) .OR. hb_FileExists( cDir + cName + ".fpt" )
         OutErr( "Refusing to overwrite " + cDir + cName + hb_eol() )
         ErrorLevel( 2 )
         RETURN
      ENDIF
   NEXT

   rddSetDefault( "DBFCDX" )
   hb_cdpSelect( "UA866" )
   SET AUTOPEN OFF
   ErrorBlock( {|e| Break( e ) } )
   BEGIN SEQUENCE
      IF cMode == "benchmark"
         MakePayments( cDir + "test_main.dbf", nRows )
      ELSE
         MakeBasic( cDir + "basic.dbf" )
         MakeSpecial( cDir + "special.dbf" )
         MakeNumbers( cDir )
         MakePayments( cDir + "empty.dbf", 0 )
         MakePayments( cDir + "single.dbf", 1 )
         MakePayments( cDir + "test_main.dbf", 1000 )
      ENDIF
   RECOVER USING oError
      CLOSE ALL
      OutErr( oError:Operation + ": " + oError:Description + hb_eol() )
      ErrorLevel( 4 )
   END SEQUENCE
RETURN

STATIC PROCEDURE CreateTable( cPath, aFields )
   dbCreate( cPath, aFields, "DBFCDX" )
   dbUseArea( .T., "DBFCDX", cPath, "FIXTURE", .F., .F., "UA866" )
RETURN

STATIC PROCEDURE MakeBasic( cPath )
   CreateTable( cPath, { { "ID", "N", 6, 0 }, { "NAME", "C", 20, 0 }, ;
      { "BIRTH", "D", 8, 0 }, { "ACTIVE", "L", 1, 0 }, { "AMOUNT", "N", 10, 2 }, { "NOTE", "M", 10, 0 } } )
   APPEND BLANK
   REPLACE ID WITH 1, NAME WITH hb_UTF8ToStr( "Алёна" ), BIRTH WITH hb_SToD( "19900102" ), ;
      ACTIVE WITH .T., AMOUNT WITH 12.50, NOTE WITH hb_UTF8ToStr( "  Ёлка  " )
   APPEND BLANK
   REPLACE ID WITH 2, NAME WITH "BOB", AMOUNT WITH -7.25, NOTE WITH Replicate( "A", 56 )
   APPEND BLANK
   REPLACE ID WITH 3, NAME WITH "CAROL", BIRTH WITH hb_SToD( "20000101" ), ;
      ACTIVE WITH .T., AMOUNT WITH 0, NOTE WITH Replicate( "B", 56 ) + "!"
   DELETE
   APPEND BLANK
   REPLACE ID WITH 4, NAME WITH "DAVE", AMOUNT WITH 0
   dbCloseArea()
RETURN

STATIC PROCEDURE MakeSpecial( cPath )
   LOCAL n, cTime
   LOCAL aTimes := { "", "20240229235959123", "20260101120001001", "20261231235959999", "20270101000000000" }
   CreateTable( cPath, { { "IDZ", "+", 4, 0 }, { "TSZ", "=", 8, 0 }, ;
      { "VMZ", "^", 8, 0 }, { "STAMP", "@", 8, 0 }, { "NAME", "C", 20, 0 } } )
   FOR n := 1 TO Len( aTimes )
      cTime := aTimes[ n ]
      APPEND BLANK
      REPLACE NAME WITH "ITEM" + hb_ntos( n )
      IF ! Empty( cTime )
         REPLACE STAMP WITH hb_SToT( cTime )
      ENDIF
   NEXT
   dbCloseArea()
RETURN

STATIC PROCEDURE MakeNumbers( cDir )
   CreateTable( cDir + "numeric.dbf", { { "ID", "N", 4, 0 }, { "AMOUNT", "N", 15, 2 }, { "RATE", "N", 15, 5 } } )
   APPEND BLANK
   REPLACE ID WITH 1, AMOUNT WITH 999999999999.99, RATE WITH 999999999.99999
   APPEND BLANK
   REPLACE ID WITH 2, AMOUNT WITH -99999999999.99, RATE WITH -99999999.99999
   APPEND BLANK
   REPLACE ID WITH 3, AMOUNT WITH 0.01, RATE WITH 0.00001
   dbCloseArea()

   CreateTable( cDir + "wide.dbf", { { "NUM", "N", 20, 0 } } )
   APPEND BLANK
   REPLACE NUM WITH 9007199254740993
   dbCloseArea()
RETURN

STATIC PROCEDURE MakePayments( cPath, nRows )
   LOCAL n, cName, cNote, nStreet, dBase := hb_SToD( "20240101" )
   LOCAL aNames := { "Алёна Орлова", "Сергей Волков", "Фёдор Лебедев", "Ольга Попова" }
   CreateTable( cPath, { { "ID", "N", 10, 0 }, { "FULL_NAME", "C", 80, 0 }, { "PAY_DATE", "D", 8, 0 }, ;
      { "PAY_AMOUNT", "N", 12, 2 }, { "CITY_ID", "N", 6, 0 }, { "STREET_ID", "N", 8, 0 }, ;
      { "HOUSE_NO", "C", 15, 0 }, { "APARTMENT", "N", 8, 0 }, { "NOTE", "M", 10, 0 }, ;
      { "IS_PRINTED", "L", 1, 0 }, { "IS_EMAILED", "L", 1, 0 }, { "PAY_TYPE", "N", 1, 0 }, { "CREATED_AT", "T", 8, 0 } } )
   cNote := hb_UTF8ToStr( "Оплата за месяц" )
   FOR n := 1 TO nRows
      nStreet := ( ( n - 1 ) % 100 ) + 1
      cName := hb_UTF8ToStr( aNames[ ( ( n - 1 ) % Len( aNames ) ) + 1 ] )
      APPEND BLANK
      REPLACE ID WITH n, FULL_NAME WITH cName, PAY_DATE WITH dBase + ( n % 731 ), ;
         PAY_AMOUNT WITH ( ( n * 37 ) % 250000 ) / 100, CITY_ID WITH ( ( nStreet - 1 ) % 10 ) + 1, ;
         STREET_ID WITH nStreet, HOUSE_NO WITH LTrim( Str( ( n % 280 ) + 1, 3, 0 ) ), APARTMENT WITH n % 150, ;
         IS_PRINTED WITH ( n % 5 != 0 ), IS_EMAILED WITH ( n % 5 < 3 ), PAY_TYPE WITH n % 3, ;
         CREATED_AT WITH hb_SToT( DToS( FIELD->PAY_DATE ) + "120000" + StrZero( n % 1000, 3 ) )
      IF n % 7 < 2
         REPLACE NOTE WITH cNote
      ENDIF
   NEXT
   dbCloseArea()
RETURN
