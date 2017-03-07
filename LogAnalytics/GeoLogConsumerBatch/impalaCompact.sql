refresh geo;
insert overwrite geo select * from geo;
refresh level;
insert overwrite level select * from level;
refresh purchase;
insert overwrite purchase select * from purchase;
