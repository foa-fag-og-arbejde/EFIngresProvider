/***** Customers ******/

DROP TABLE Customers;
CREATE TABLE Customers (
    CustomerID   VARCHAR(5)  NOT NULL,
    CompanyName  VARCHAR(40) NOT NULL,
	ContactName  VARCHAR(30),
	ContactTitle VARCHAR(30),
	Address      VARCHAR(60),
	City         VARCHAR(32),
	Region       VARCHAR(15),
	PostalCode   VARCHAR(10),
	Country      VARCHAR(15),
	Phone        VARCHAR(24),
	Fax          VARCHAR(24),
    ContactDate  ingresdate,
    CONSTRAINT PK_Customers PRIMARY KEY (CustomerID)
);

GRANT ALL ON Customers TO PUBLIC;

/***** IngresDateTest ******/

DROP TABLE IngresDateTest;
CREATE TABLE IngresDateTest (
    Name           VARCHAR(30) NOT NULL,
    Value          ingresdate  WITH NULL,
    FormattedValue VARCHAR(30) NOT NULL,
    CONSTRAINT PK_IngresDateTest PRIMARY KEY (Name)
);

GRANT ALL ON IngresDateTest TO PUBLIC;

/***** IngresDateTest ******/

DROP TABLE IngresDateTest2;
CREATE TABLE IngresDateTest2 (
    Value ingresdate  WITH NULL
);

GRANT ALL ON IngresDateTest2 TO PUBLIC;

/***** DateTypesTest ******/

DROP TABLE DateTypesTest;
CREATE TABLE DateTypesTest (
    IDate      ingresdate     WITH NULL,
	ADate      ansidate       WITH NULL,
	ATime      time           WITH NULL,
	ATimeStamp timestamp      WITH NULL
);

GRANT ALL ON DateTypesTest TO PUBLIC;

/***** ErrorTest ******/

DROP TABLE ErrorTest;
CREATE TABLE ErrorTest (
    ID    VARCHAR(5)  NOT NULL,
    Name  VARCHAR(40) NOT NULL,
    CONSTRAINT PK_ErrorTest PRIMARY KEY (ID)
);

GRANT ALL ON ErrorTest TO PUBLIC;

/***** Entity tests *****/

DROP TABLE ansaettelse;
CREATE TABLE ansaettelse (
    medl_ident          integer4   NOT NULL WITH DEFAULT,
    lbnr                integer4   NOT NULL WITH DEFAULT,
    arbst_nr            integer4   NOT NULL WITH DEFAULT,
    fra_dato            ingresdate NOT NULL WITH DEFAULT,
    til_dato            ingresdate NOT NULL WITH DEFAULT,
    form                char(1)    NOT NULL WITH DEFAULT,
    arbejds_time        money      NOT NULL WITH DEFAULT,
    primaer_ansaettelse char(1)    NOT NULL WITH DEFAULT,
    reg_tid             ingresdate NOT NULL WITH DEFAULT,
    reg_init            char(12)   NOT NULL WITH DEFAULT,
    reg_vers_nr         integer4   NOT NULL WITH DEFAULT
)
WITH NODUPLICATES;

MODIFY ansaettelse TO btree UNIQUE ON medl_ident, lbnr;

ALTER TABLE ansaettelse
    ADD CONSTRAINT c_ansat_medl_ident
      CHECK(medl_ident > 0);

ALTER TABLE ansaettelse
    ADD CONSTRAINT c_ansat_primaer_ansaette
      CHECK(primaer_ansaettelse in ('j', 'n'));

CREATE TABLE k_ansaettelse (
    medl_ident INTEGER4 NOT NULL WITH DEFAULT,
    lbnr INTEGER4 NOT NULL WITH DEFAULT,
    arbst_nr INTEGER4 NOT NULL WITH DEFAULT,
    fra_dato DATE NOT NULL WITH DEFAULT,
    til_dato DATE NOT NULL WITH DEFAULT,
    form CHAR(1) NOT NULL WITH DEFAULT,
    arbejds_time MONEY NOT NULL WITH DEFAULT,
    primaer_ansaettelse CHAR(1) NOT NULL WITH DEFAULT,
    kstart_tid DATE NOT NULL WITH DEFAULT,
    reg_init CHAR(12) NOT NULL WITH DEFAULT,
    reg_vers_nr INTEGER4 NOT NULL WITH DEFAULT,
    kslut_tid DATE NOT NULL WITH DEFAULT,
    kstatus CHAR(1) NOT NULL WITH DEFAULT
) WITH NODUPLICATES;

MODIFY k_ansaettelse TO btree UNIQUE ON
    medl_ident,
    lbnr,
    reg_vers_nr;

CREATE PROCEDURE p_ansaettelse (
    medl_ident INTEGER4 NOT NULL,
     lbnr INTEGER4 NOT NULL,
     arbst_nr INTEGER4 NOT NULL,
     fra_dato DATE NOT NULL,
     til_dato DATE NOT NULL,
     form CHAR(1) NOT NULL,
     arbejds_time MONEY NOT NULL,
     primaer_ansaettelse CHAR(1) NOT NULL,
     kstart_tid DATE NOT NULL,
     reg_init CHAR(12) NOT NULL,
     reg_vers_nr INTEGER4 NOT NULL,
     kslut_tid DATE NOT NULL,
     kstatus CHAR(1) NOT NULL,
     ny_reg_init CHAR(12) NOT NULL,
     ny_reg_vers_nr INTEGER4 NOT NULL)
  AS
  declare zzzzzz_lav_kopi = integer4 not null;
  begin
    zzzzzz_lav_kopi = 0;

    if kstatus = 'O' then /* Opret - insert */
      zzzzzz_lav_kopi = 0;
    endif;

    if kstatus = 'R' then /* Ret - update */
      if ny_reg_vers_nr != reg_vers_nr + 1 then
        raise error 1001 'Historik fejl: Reg_vers_nr er ikke ændret';
        return;
      endif;
      zzzzzz_lav_kopi = 1;
    endif;

    if kstatus = 'S' then /* Slet - delete */
      zzzzzz_lav_kopi = 2;
    endif;

    if zzzzzz_lav_kopi = 0 then
      /* der skal ikke laves nogen kopi */
      return;
    endif;
    INSERT INTO "fiksdba".k_ansaettelse VALUES
      (:medl_ident, :lbnr, :arbst_nr, :fra_dato, :til_dato,
	 :form, :arbejds_time, :primaer_ansaettelse,
	 :kstart_tid, :reg_init, :reg_vers_nr, :kslut_tid,
	 :kstatus);
    if zzzzzz_lav_kopi = 2 then
       INSERT INTO "fiksdba".k_ansaettelse VALUES
      (:medl_ident, :lbnr, :arbst_nr, :fra_dato, :til_dato,
	 :form, :arbejds_time, :primaer_ansaettelse, :kslut_tid
	 + '1 sec', dbmsinfo('username'), :reg_vers_nr+1,
	 :kslut_tid, '-');
    endif;
  END;

CREATE RULE r_ansaettelse_r AFTER UPDATE OF ansaettelse
    REFERENCING OLD AS gl NEW AS ny
    EXECUTE PROCEDURE p_ansaettelse(medl_ident = gl.medl_ident,
      lbnr = gl.lbnr, arbst_nr = gl.arbst_nr, fra_dato =
      gl.fra_dato, til_dato = gl.til_dato, form = gl.form,
      arbejds_time = gl.arbejds_time, primaer_ansaettelse =
      gl.primaer_ansaettelse, kstart_tid = gl.reg_tid, reg_init
      = gl.reg_init, reg_vers_nr = gl.reg_vers_nr, kslut_tid =
      ny.reg_tid, kstatus = 'R', ny_reg_vers_nr =
      ny.reg_vers_nr, ny_reg_init = ny.reg_init);

CREATE RULE r_ansaettelse_s AFTER DELETE OF ansaettelse
    REFERENCING OLD AS gl NEW AS ny
    EXECUTE PROCEDURE p_ansaettelse(medl_ident = gl.medl_ident,
      lbnr = gl.lbnr, arbst_nr = gl.arbst_nr, fra_dato =
      gl.fra_dato, til_dato = gl.til_dato, form = gl.form,
      arbejds_time = gl.arbejds_time, primaer_ansaettelse =
      gl.primaer_ansaettelse, kstart_tid = gl.reg_tid, reg_init
      = gl.reg_init, reg_vers_nr = gl.reg_vers_nr, kslut_tid =
      'NOW', kstatus = 'S', ny_reg_vers_nr = ny.reg_vers_nr,
      ny_reg_init = ny.reg_init);

