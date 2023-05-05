import Env
import numpy as np
import matplotlib.pyplot as plt
import math

def SellPutPrice(price, exp, std, numOfStd = 1):
    return price * (100 - exp - std * numOfStd)/100.0

def Distribution(symbol, data):
    # canonicalize the 0.5
    cannonicalData = []
    for d in data:
        cannonicalData.append(int(d * 20)/20.0)

    possibilities = {}
    for d in cannonicalData:
        if(d not in possibilities):
            possibilities[d] = 0
        possibilities[d] = possibilities[d] + 1

    exp = 0.0
    dataLength = len(cannonicalData)
    for k,v in possibilities.items():
        exp += k * v / dataLength
    print(f"symbol:{symbol}")
    print("μ:" + str(exp))
    print("max:" + str(max(cannonicalData)))
    print("min:" + str(min(cannonicalData)))

    
    print("avg:" + str(np.mean(cannonicalData)))
    print("std:" + str(np.var(cannonicalData)))
    stdValue = np.std(cannonicalData)
    print("δ:" + str(stdValue))

def ReadVolatility(symbol):
    path = Env.GetDataRoot() +"\\volatility\\" + symbol +"weekly.csv"
    data = None
    with open(path, "r") as f:
        data = np.loadtxt(f, delimiter = ",", usecols=(1), skiprows=1)
    return data


def main():
    symbol = "IWM"
    data = ReadVolatility(symbol)
    Distribution(symbol, data)

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print("main get error:" + e)