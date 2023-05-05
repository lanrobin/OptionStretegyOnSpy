import csv
import datetime
import Env

def calculate_volatility(input_file, output_file):
    # define the date format used in the input file
    date_format = "%Y-%m-%d"

    # define the header for the output file
    output_header = ["Date", "Volatility", "Close"]

    # read the input file into a list of dictionaries
    with open(input_file, "r") as f:
        reader = csv.DictReader(f)
        data = [row for row in reader]

    # convert the date strings to datetime objects
    for row in data:
        row["Date"] = datetime.datetime.strptime(row["Date"], date_format)

    # sort the data by date in ascending order
    data.sort(key=lambda row: row["Date"])

    # group the data by natural weeks
    weekly_data = []
    current_week_data = []
    for i, row in enumerate(data):
        current_week_data.append(row)
        if i == len(data) - 1 or row["Date"].weekday() == 4:
            # this is the last trading day of the week, or the last row in the data
            weekly_data.append(current_week_data)
            current_week_data = []

    # calculate the weekly volatility for each week
    output_data = []
    previous_week = None
    for i, week in enumerate(weekly_data):
        if previous_week is None:
            previous_week = week
            continue

        current_close = float(week[-1]["Close"])
        previous_close = float(previous_week[-1]["Close"])
        volatility = (current_close - previous_close) / previous_close * 100

        # add the output row to the list
        output_row = {
            "Date": week[-1]["Date"].strftime(date_format),
            "Volatility": volatility,
            "Close": week[-1]["Close"],
        }
        output_data.append(output_row)
        previous_week = week

    # write the output file
    with open(output_file, "w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=output_header)
        writer.writeheader()
        writer.writerows(output_data)

if __name__ == "__main__":
    s = "IWM"
    input_file = f"{Env.GetDataRoot()}/history/{s}_unadjusted.csv"
    output_file = f"{Env.GetDataRoot()}/volatility/{s}weekly.csv"
    calculate_volatility(input_file, output_file)
