# set window parameters 
set term qt title ARG2

# calculate statistics
stats ARG1 using 1 name "Select" 
stats ARG1 using 2 name "InsertOne" 
stats ARG1 using 3 name "InsertMany" 

# prepare view
set encoding utf8
set title "Request dynamic"
set xlabel "Iterations"
set ylabel "Elapsed milliseconds"
set grid

# plot data
plot ARG1 using 1 with lines title "Select", \
				Select_mean	with lines lw 2 dt (10, 10) title "Select Mean", \
	 ARG1 using 2 with lines title "Insert One", \
				InsertOne_mean with lines lw 2 dt (10, 10) title "Insert One Mean", \
	 ARG1 using 3 with lines title "Insert Many", \
				InsertMany_mean with lines lw 2 dt (10, 10) title "Insert Many Mean"

# do not close the window automaticly
pause mouse close