import platform
import logging
from pathlib import Path
import json
import smtplib
from email.header import Header 
from email.mime.text import MIMEText
import re
from datetime import datetime
import sys
import ssl
import FileUtil

def GetDataRoot():
    path = ""
    if platform.system().lower() == 'windows':
        path = "D:\\stock"
    elif platform.system().lower() == 'linux':
        path = "/datadrive/stock"
    else:
        path = "/datadrive/stock"
    # Ensure the folder exists.
    Path(path).mkdir(parents=True, exist_ok=True)
    return path

def SetupLogging(fileName):
    logPath = GetDataRoot() + "/log/"
    FileUtil.EnsurePathExists(logPath)
    time_rotating_file_handler = logging.handlers.TimedRotatingFileHandler(filename= logPath +"/"+fileName + '.log', when='D', interval=1, backupCount=7, encoding='utf-8')
    time_rotating_file_handler.setLevel(logging.DEBUG)
    logging.basicConfig(level=logging.INFO,
                format='%(asctime)s %(levelname)s %(message)s',
                datefmt='%Y-%m-%d %H:%M:%S',
                # filename=file_name,
                # filemode='a',
                handlers=[time_rotating_file_handler, logging.StreamHandler(sys.stdout)],
                )

def IsStockMarketOpenToday(d):
    weekno = d.weekday()
    if(weekno > 4):
        return False
    return d.strftime("%Y-%m-%d") not in MarketCloseDate


# from here https://www.standard.com/individual/retirement/stock-market-and-bank-holidays
# https://www.nyse.com/markets/hours-calendars
MarketCloseDate = {
    "2022-11-24",
    "2022-12-26",
    "2023-01-02",
    "2023-01-16",
    "2023-02-20",
    "2023-04-07",
    "2023-05-29",
    "2023-06-19",
    "2023-07-04",
    "2023-09-04",
    "2023-11-23",
    "2023-12-25",
    "2024-03-01",
    "2024-01-15",
    "2024-02-19",
    "2024-03-29",
    "2024-05-27",
    "2024-06-19",
    "2024-07-04",
    "2024-09-02",
    "2024-11-28",
    "2024-12-25",
    "2025-01-01",
    "2025-01-20",
    "2025-02-17",
    "2025-04-18",
    "2025-05-26",
    "2025-06-19",
    "2025-07-04",
    "2025-09-01",
    "2025-11-27",
    "2025-12-25"
}

# Good Friday不开市，所以应该是周四
NonFridayExpireDates = {
    "2023-04-06",
    "2024-03-28"
}

SpecialMarketCloseDate = {
    "2023-11-24":"2023-11-24:130000",
    "2024-11-29":"2023-11-29:130000",
}