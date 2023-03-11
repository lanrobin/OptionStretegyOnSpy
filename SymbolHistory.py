import yfinance as yf
import multiprocessing
import logging
from logging.handlers import RotatingFileHandler
from concurrent.futures import ThreadPoolExecutor, ProcessPoolExecutor
import pandas as pd
import Env
import FileUtil

def GetOneStockHistory(sym, autoAdjust = False):
    try:
        date = pd.Timestamp.now()
        s = yf.Ticker(sym)
        dateStr = date.strftime("%Y-%m-%d")
        print("Begin to fetch history of " + sym)
        data = s.history(interval="1d", start="1990-1-1", end=dateStr, auto_adjust=autoAdjust)
        historyDir = Env.GetDataRoot() +"/history/"
        FileUtil.EnsurePathExists(historyDir)
        fileName = historyDir + sym +".csv"
        if(not autoAdjust):
            fileName = historyDir + sym +"_unadjusted.csv"
        with open(fileName, "w") as f:
            f.writelines("Date," + ",".join(map(str, data.columns))+ "\n")
            for index, item in zip(data.index.date, data.values):
                f.writelines(str(index) +",")
                f.writelines(",".join(map(str, item))+ "\n")
        print("Finished to fetch " + sym)
    except Exception as e:
        print(f"Fectch {sym} got error: {e}")

def main():
    with open(Env.GetDataRoot() +"/symbols.txt", encoding="utf-8") as s:
        Env.SetupLogging("stock_download")
        lines = s.read().splitlines()
        num_cores = multiprocessing.cpu_count()
        logging.info("There are " + str(num_cores) + " CPU(s) on this computer.")
        with ThreadPoolExecutor(5 * num_cores) as executor:
            results = executor.map(GetOneStockHistory, lines)
            for result in results:
                logging.info("Result:" + str(result))
            #for i in lines:
            #    GetOneStockHistory(i)
            logging.info("Done")
if __name__ == "__main__":
    main()