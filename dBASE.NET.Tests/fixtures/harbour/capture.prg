#include "fileio.ch"

REQUEST DBFCDX, DBFFPT, HB_CODEPAGE_UA866, HB_CODEPAGE_UTF8

PROCEDURE Main( cPath, cJson )
   LOCAL aFields := {}, aRows := {}, aValues, aField, nRow, nField, cHeader, oError, hFixture

   IF Empty( cPath ) .OR. Empty( cJson ) .OR. hb_FileExists( cJson )
      OutErr( "Usage: capture <dbf path> <new JSON path>" + hb_eol() )
      ErrorLevel( 2 )
      RETURN
   ENDIF

   hb_cdpSelect( "UA866" )
   rddSetDefault( "DBFCDX" )
   SET AUTOPEN OFF
   SET DELETED OFF
   ErrorBlock( {|e| Break( e ) } )
   BEGIN SEQUENCE
      USE ( cPath ) NEW SHARED READONLY VIA "DBFCDX" CODEPAGE "UA866"
      cHeader := ReadDescriptors( cPath, FCount() )
      FOR EACH aField IN dbStruct()
         nField := aField:__enumIndex()
         AAdd( aFields, { "name" => aField[ 1 ], "type" => aField[ 2 ], ;
            "descriptor" => SubStr( cHeader, 44 + ( nField - 1 ) * 32, 1 ), ;
            "length" => aField[ 3 ], "decimals" => aField[ 4 ] } )
      NEXT

      FOR nRow := 1 TO LastRec()
         dbGoTo( nRow )
         aValues := Array( Len( aFields ) )
         FOR nField := 1 TO Len( aFields )
            aValues[ nField ] := ExportValue( FieldGet( nField ), aFields[ nField ] )
         NEXT
         AAdd( aRows, { "record" => RecNo(), "deleted" => Deleted(), "values" => aValues } )
      NEXT

      hFixture := { "harbour" => Version(), "rdd" => RddName(), "codePage" => "UA866", ;
         "dbfVersion" => Asc( cHeader ), "ldid" => Asc( SubStr( cHeader, 30, 1 ) ), ;
         "fields" => aFields, "rows" => aRows }
      dbCloseArea()
      IF ! hb_MemoWrit( cJson, hb_jsonEncode( hFixture ) + Chr( 10 ) )
         Break( "Cannot write " + cJson )
      ENDIF
   RECOVER USING oError
      CLOSE ALL
      OutErr( iif( ValType( oError ) == "C", oError, oError:Description ) + hb_eol() )
      ErrorLevel( 4 )
   END SEQUENCE
RETURN

STATIC FUNCTION ReadDescriptors( cPath, nFields )
   LOCAL nFile := FOpen( cPath, FO_READ + FO_SHARED )
   LOCAL nBytes := 32 * ( nFields + 1 ), cBytes
   IF nFile < 0
      Break( "Cannot open the DBF header" )
   ENDIF
   cBytes := Space( nBytes )
   IF FRead( nFile, @cBytes, nBytes ) != nBytes
      FClose( nFile )
      Break( "Incomplete DBF header" )
   ENDIF
   FClose( nFile )
RETURN cBytes

STATIC FUNCTION ExportValue( xValue, hField )
   SWITCH ValType( xValue )
   CASE "C"
   CASE "M"
      RETURN hb_StrToUTF8( xValue )
   CASE "D"
      RETURN iif( Empty( xValue ), NIL, DToS( xValue ) )
   CASE "T"
      RETURN iif( Empty( xValue ), NIL, hb_TToS( xValue ) )
   CASE "N"
      RETURN LTrim( Str( xValue, Max( hField[ "length" ] + 1, 21 ), hField[ "decimals" ] ) )
   ENDSWITCH
RETURN xValue
