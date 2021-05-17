CREATE TABLE stress_test
(
	int8		smallint			NOT NULL,
	int16		smallint			NOT NULL,
	int32		integer				NOT NULL,
	int64		bigint				NOT NULL,
	float		real				NOT NULL,
	datetime	timestamp			NOT NULL,
	string_1	char(250)			NOT NULL,
	string_2	char(24)			NOT NULL
);

CREATE INDEX IX_string_1_hash ON stress_test USING HASH(string_1);
CREATE INDEX IX_string_2_hash ON stress_test USING HASH(string_2);