import System.IO

files = [5, 10, 15, 20, 25, 35, 45, 55, 65, 85, 105, 125, 145, 165, 185, 205, 230]

main = do
       writeFile "output.dat" ""
       mapM_ writeOutput files


writeOutput file = do 
    contents <- readFile ("bench"++ show(file) ++ ".csv")
    let list = createList contents
    let minFPS = 1000.0 / (maximum list)
    let maxFPS = 1000.0 / (minimum list)
    let avgFPS = 1000.0 / ((sum list)/(realToFrac(length list)))
    appendFile "output.dat" (show(file) ++ " " ++ formatValues avgFPS minFPS maxFPS)

createList contents = map (\ls -> ls!!2) (map strToList (tail (tail (lines contents))) )

formatValues x y z = show(x) ++ " " ++ show(y) ++ " " ++ show(z) ++ "\n"

strToList str = map read (words str)