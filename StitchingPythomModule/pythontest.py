import cv2 as cv

def initfunction ():
    img = cv.imread("C:/Users/cocuy/Pictures/Screenshots/Screenshot 2023-06-27 210031.png")

    cv.imshow("Display window", img)
    k = cv.waitKey(0)
