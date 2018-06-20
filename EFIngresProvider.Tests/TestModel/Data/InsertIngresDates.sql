insert into IngresDateTest values ('Null', null, '');
insert into IngresDateTest values ('Empty', date(''), 'date('''')');
insert into IngresDateTest values ('Date', date('2011-12-02'), 'date(''2011-12-02'')');
insert into IngresDateTest values ('DateTime', date('2011-12-02 09:53:28'), 'date(''2011-12-02 09:53:28'')');

-- insert into IngresDateTest values ('Interval years', date('2 years 11 months 2 days 5 hours 4 minutes 3 seconds'), 'date(''2 yrs 11 mos 2 days 5 hrs'')');
-- insert into IngresDateTest values ('Interval hours', date('59 hours 53 minutes 28 seconds'), 'date(''2 days 11 hrs 53 mins 28 secs'')');

insert into DateTypesTest (Idate, ADate, ATime, ATimeStamp) values ('2011-12-02 09:53:28', '2011-12-02', '09:53:28', '2011-12-02 09:53:28');
