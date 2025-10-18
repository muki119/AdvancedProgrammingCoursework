
module SumFloat
   let rec sum(list:list<double>) =
        match list with
        | (head:double) :: (tail:list<double>) -> head + sum(tail)
        | [] -> 0.0


  

    

