CREATE TABLE stress_test
(
	id			UUID				NOT NULL,
	int8		Int8 				NOT NULL,
	int16		Int16				NOT NULL,
	int32		Int32				NOT NULL,
	int64		Int64				NOT NULL,
	decimal		Decimal32(0)		NOT NULL,
	datetime	timestamp			NOT NULL,
	string_1	FixedString(250)	NOT NULL,
	string_2	FixedString(24)		NOT NULL,
	boolean		Boolean				NOT NULL
)
	ENGINE = MergeTree()
	PRIMARY KEY(id);