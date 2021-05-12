CREATE TABLE stress_test
(
	id			uuid				PRIMARY KEY,
	int8		smallint			NOT NULL,
	int16		smallint			NOT NULL,
	int32		integer				NOT NULL,
	int64		bigint				NOT NULL,
	decimal		numeric				NOT NULL,
	datetime	timestamp			NOT NULL,
	string_1	char(250)			NOT NULL,
	string_2	char(24)			NOT NULL,
	boolean		boolean				NOT NULL
);