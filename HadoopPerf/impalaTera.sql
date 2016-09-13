drop table if exists teratable;

create external table teratable
(
    key char(10),
    rowid char(10),
    filler char(78)
)
location '/benchmarks/tera/out';
