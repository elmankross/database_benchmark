	SELECT	*
	FROM	stress_test
	WHERE	string_1 = @string_1
UNION ALL
	SELECT	*
	FROM	stress_test
	WHERE	string_2 = @string_2;