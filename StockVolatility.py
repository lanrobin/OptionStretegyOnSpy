import pandas as pd
import math
import Env
import FileUtil

SymbolName = "TSLA"

def GetWeeklyVolatility(filePath, excludeDivends = False, AdjustedData = True):
    result = []
    with open(filePath) as f:
        lines = f.read().splitlines()[1:]
        startingPrice = 0
        endPrice = 0
        weeklyHigh = 0
        weeklyLow = 0
        isTheFirstDayOfWeek = False
        lastDayOfWeek = -1
        lastDayStr = ''
        dateStr = ''
        dividend = 0
        result.append(['Date', 'Volatility', 'Open', 'Close', 'WeeklyHigh', 'WeeklyLow', 'Dividend', 'MaxDecrease', 'MaxIncrease'])
        for line in lines:
            parts = line.split(',')
            date = pd.Timestamp(parts[0])
            dateStr = parts[0]
            v = 0
            maxIncrease = 0
            maxDecrease = 0
            if date.dayofweek < lastDayOfWeek:
                if(excludeDivends):
                    v = (endPrice - dividend - startingPrice)/startingPrice * 100
                    maxIncrease = (weeklyHigh - dividend - startingPrice)/ startingPrice * 100
                    maxDecrease = (weeklyLow - dividend - startingPrice)/startingPrice * 100
                else:
                    v = (endPrice - startingPrice)/startingPrice * 100
                    maxIncrease = (weeklyHigh - startingPrice)/ startingPrice * 100
                    maxDecrease = (weeklyLow - startingPrice)/startingPrice * 100
                #print(v)
                result.append([lastDayStr, v, startingPrice, endPrice, weeklyHigh, weeklyLow, dividend, maxDecrease, maxIncrease])
                isTheFirstDayOfWeek = False
                lastDay = -1
                startingPrice = 0
                endPrice = 0
                weeklyHigh = 0
                weeklyLow = 0
                dividend = 0

            ## Find the weekly open price.
            if not isTheFirstDayOfWeek:
                isTheFirstDayOfWeek = True
                startingPrice = float(parts[1])
                weeklyHigh = float(parts[2])
                weeklyLow = float(parts[3])
            
            weeklyHigh = max(weeklyHigh, float(parts[2]))
            weeklyLow = min(weeklyLow, float(parts[3]))
            if(AdjustedData):
                dividend += float(parts[6])
            else:
                dividend += float(parts[7])
            lastDayOfWeek = date.dayofweek
            lastDayStr = dateStr
            endPrice = float(parts[4])
        if isTheFirstDayOfWeek:
            ## This is the last week.
            if(excludeDivends):
                v = (endPrice - dividend - startingPrice)/startingPrice * 100
                maxIncrease = (weeklyHigh - dividend - startingPrice)/ startingPrice * 100
                maxDecrease = (weeklyLow - dividend - startingPrice)/startingPrice * 100
            else:
                v = (endPrice - startingPrice)/startingPrice * 100
                maxIncrease = (weeklyHigh - startingPrice)/ startingPrice * 100
                maxDecrease = (weeklyLow - startingPrice)/startingPrice * 100
            #print(v)
            result.append([lastDayStr, v, startingPrice, endPrice, weeklyHigh, weeklyLow, dividend, maxDecrease, maxIncrease])
    return result


def WriteToFile(values, fileName):
    with open(fileName, "w") as f:
        for l in values:
            f.writelines(",".join(map(str, l)) + "\n")

def main():
    v = GetWeeklyVolatility(Env.GetDataRoot() + "\\history\\"+ SymbolName +"_unadjusted.csv", excludeDivends=False, AdjustedData=False)

    weeklyVolatilityDir = Env.GetDataRoot() +"\\volatility"
    FileUtil.EnsurePathExists(weeklyVolatilityDir)

    WriteToFile(v, weeklyVolatilityDir +"\\"+ SymbolName +"weekly.csv")

if __name__ == "__main__":
    main()