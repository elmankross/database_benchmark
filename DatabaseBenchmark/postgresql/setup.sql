CREATE TABLE stress_test
(
	int8_1		smallint			NOT NULL,
	int8_2		smallint			NOT NULL,
	int16_1		smallint			NOT NULL,
	int16_2		smallint			NOT NULL,
	int32_1		integer				NOT NULL,
	int32_2		integer				NOT NULL,
	int64_1		bigint				NOT NULL,
	int64_2		bigint				NOT NULL,
	float_1		real				NOT NULL,
	float_2		real				NOT NULL,
	datetime_1	timestamp			NOT NULL,
	datetime_2	timestamp			NOT NULL,
	string_1	char(250)			NOT NULL,
	string_2	char(24)			NOT NULL,

	PRIMARY KEY (float_1, string_1, string_2)
);

CREATE INDEX IX_string_1_hash ON stress_test USING HASH(string_1);
CREATE INDEX IX_string_2_hash ON stress_test USING HASH(string_2);