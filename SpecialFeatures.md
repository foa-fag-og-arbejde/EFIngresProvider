# Special features in EFIngresProvider

EFIngresProvider was originally developed to support FOA's main database (_fiksdb_),
which is at least 20 years old and contains over 1600 tables and 200 views.

To make the provider work well with this database I introduced some special features.

## Contents

<!-- toc -->

- [EFIngresConnection](#efingresconnection)
  * [Optimistic locking](#optimistic-locking)
  * [Connection string parameters](#connection-string-parameters)
    + [TrimChars](#trimchars)
    + [JoinOPGreedy](#joinopgreedy)
    + [JoinOPTimeout](#joinoptimeout)
    + [UseIngresDate](#useingresdate)
- [EFIngresCommand](#efingrescommand)
- [IngresDate](#ingresdate)
  * [Formatting ingres dates](#formatting-ingres-dates)
  * [Intervals and TimeSpans](#intervals-and-timespans)
- [Database models](#database-models)
  * [Views are modelled as tables](#views-are-modelled-as-tables)
  * [Synthetic primary keys](#synthetic-primary-keys)

<!-- tocstop -->

## EFIngresConnection

EFIngresProvider implements its own `DbConnection`, that wraps `IngresConnection` from `Ingres.Client`.

### Optimistic locking

We always use optimistic locking, so each time `EFIngresConnection` opens a connection, the following
command is executed:

```
SET LOCKMODE session WHERE readlock=nolock
```

This ought to be changed, so it is controlled by a connection string parameter.

### Connection string parameters

#### TrimChars

If set to true, any `char` and `nchar` columns retrieved will have trailing blanks removed.

_Motivation_

_fiksdb_ contains a lot of `char` columns, that, in hinsight, would have been better represented
as `varchar`. In the standard ADO.NET provider these columns are, correctly, return with trailing
blanks to pad to the column's length. I our .Net applications we did not want this.

#### JoinOPGreedy

If set to true, the connection will run `SET JOINOP GREEDY` every time it is opened.

_Motivation_

Performance

#### JoinOPTimeout

If set to a value greater than 0, the connection will run `SET JOINOP TIMEOUT {value}` every time it is opened.

_Motivation_

Performance

#### UseIngresDate

If set to true, Ingres dates are retrieved as `EFIngresProvider.IngresDate`, in stead og `System.DateTime`.
This does **_not_** work with entities - it only works when using `EFIngresProvider.EFIngresCommand` directly.

_Motivation_

See [IngresDate](#ingresdate)

## EFIngresCommand

EFIngresProvider implements its own `DbCommand`, that wraps `IngresCommand` from `Ingres.Client`.

Each time a command is executed the sql gets modified:

* Numbered parameters (for example `"p0"`) get converted to `"?"`.

* Any `EFIngresProvider.IngresDate`, `System.DateTime` or `System.TimeSpan` parameters get replaced using
  `EFIngresProvider.IngresDate.Format` (see [IngresDate](#ingresdate)).

* Any tables with schema `EFIngres` (for example `EFIngres.EFIngresTables`) get converted to special session
  tables, that are automatically populated using catalog helpers (see class `EFIngresProvider.Helpers.IngresCatalogs.CatalogHelpers`).   
  These tables are used when updating an `edmx` data model from a database as defined in `EFIngresProvider/Resources/EFIngresProviderServices.StoreSchemaDefinition.ssdl`.   
  _I'm pretty sure there is a better way of doing this._

## IngresDate

All dates in _fiksdb_ are represented as ingres dates. I implemented type `EFIngresProvider.IngresDate` to have
semantics as close to ingres dates as possible, so that it can handle date times, intervals and empty dates.

Sadly, I could only make this work for ADO.NET, not for Entity Framework. I could not find a way to use
user defined types in Entity Framework.

### Formatting ingres dates

Ingres dates have a quirk, that means that for example `date('2018_06_22')` and `date('2018_06_22 00:00:00')` are stored
differently.

For example, this SQL script

```sql
declare global temporary table session.dates (
    value date with null
)
on commit preserve rows
with norecovery;

insert into session.dates (value) values (date('2018_06_22'));
insert into session.dates (value) values (date('2018_06_22 00:00:00'));

select * from session.dates;
```

Returns the following:

| value                 |
| --------------------- |
| `22.06.2018`          |
| `22.06.2018 00:00:00` |

To handle this and to handle time spans and empty dates, `IngresDate` implements static method `Format`, that formats an ingres date to be used in sql sent to the ingres server.

For example:

| Value                              | Formatted                     |
| :--------------------------------- | :---------------------------- |
| `IngresDate.Empty`                 | `date('')`                    |
| DateTime: >= `9998-12-31 23:59:59` | `date('')`                    |
| DateTime: `2018-06-22`             | `date('2018_06_22')`          |
| DateTime: `2018-06-22 10:21:43`    | `date('2018_06_22 10:21:43')` |
| TimeSpan: `21 minutes 43 seconds`  | `date('21 mins 43 secs')`     |

This method is used by `EFIngresCommand` to format parameters of types `IngresDate`, `System.DateTime` and `System.TimeSpan`.

### Intervals and TimeSpans

As noted above, I was not able to make Entity Framework use `IngresDate` directly.
To handle ingres date intervals the `IngresDate` constructor converts any `System.DateTime` value less than or equal to `1500-01-01` to the
`System.TimeSpan` value calculated by subtracting `1000-01-01` from the value.

It also defines static method `ToTimeSpan`, that converts a value according to the following rules:

| Value                                            | Result                 |
| :----------------------------------------------- | :--------------------- |
| `IngresDate.Empty`                               | `System.TimeSpan.Zero` |
| TimeSpan                                         | `value`                |
| DateTime: <= `1500-01-01`                        | `value - 1000-01-01`   |
| DateTime: > `1500-01-01` < `9998-12-31 23:59:59` | `value - value.Date`   |
| DateTime: >= `9998-12-31 23:59:59`               | `System.TimeSpan.Zero` |

## Database models

In FOA we use the database first approach to making Entity Framework models as `.edmx` files.

I have implemented a couple of features to make the models work well with _fiksdb_.
These features are always enabled, but I imagine they could be turned on and of using parameters
in the connection string.

### Views are modelled as tables

When modelling a view, Entity Framework adds a select statement to the `.edmx` file. This makes for
some unwieldy sql being genereted for queries using the view.

For this reason we decided to model views as tables.

### Synthetic primary keys

_fiksdb_ has a number of tables, that do not have any keys (they are structured as `heap`). All of these
tables have the first column as a _logical key_.

To support this, the provider designates the first column as a primary key for tables and views that
do not have a key.
