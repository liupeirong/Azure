# 
# this app uses Gradient Boosted Tree to predict the remaining useful life of turbine engines
# download data to hdfs in datadir from NASA: https://ti.arc.nasa.gov/c/6/

  datadir <- "/user/pliu/CMAPSS/"
	hdfsFS <- RxHdfsFileSystem() 
  
# import training data
	train_raw <- RxTextData(file = paste(datadir, "train_FD001.txt", sep=""), fileSystem = hdfsFS)
	train_rawds <- rxImport(inData = train_raw)
	colnames <- c("id","cycle","setting1","setting2","setting3","s1","s2","s3","s4","s5","s6","s7",
	               "s8","s9","s10","s11","s12","s13","s14","s15","s16","s17","s18","s19","s20","s21")
	names(train_rawds) <- colnames
	
	library(plyr)
# find the max cycle value per engine id
	train_maxcycle <- ddply(train_rawds,~id,summarise,max=max(cycle))
	train_labled <- merge(train_rawds,train_maxcycle,by=c("id"))
# remaining useful life (RUL) = max cycle - current cycle
	train_labled$RUL <- train_labled$max - train_labled$cycle
	train_labled <- train_labled[,-which(names(train_labled) == "max")]
# the features we will build the model on, and the label	
	train_df <- train_labled[, c("cycle", "s9", "s11", "s14", "s15", "RUL")]
  
# configure the model, specifying label column vs features
	formula <- as.formula("RUL ~ cycle + s9 + s11 + s14 + s15")
# train the model
	bt <- rxBTrees(
    formula = formula, 
    data = train_df, 
    minBucket = 10, 
    minSplit = 10, 
    learningRate = 0.2, 
    seed = 5,
    nTree = 100, 
    lossFunction = "gaussian"  # this loss function doesn't exist in Spark
    )
	
# import test data, same structure as training data
	test_raw <- RxTextData(file = paste(datadir, "test_FD001.txt", sep=""), fileSystem = hdfsFS)
	test_rawds <- rxImport(inData = test_raw)
	names(test_rawds) <- colnames
# only need to predict the RUL from the latest cycle	
	test_maxcycle <- merge(aggregate(cycle~id,test_rawds,function(x) x[which.max(x)]),test_rawds)
# order the data by engine id to be merged with the ground truth for test data in the RUL data file
	test_ordered = test_maxcycle[with(test_maxcycle, order(id)), ]
	test_ds <- test_ordered[, c("cycle", "s9", "s11", "s14", "s15")]
	
# import ground truth for test data, assuming engine 1 to N, that can be concat with the ordered testing data
	rul_raw <- RxTextData(file = paste(datadir, "RUL_FD001.txt", sep=""), fileSystem = hdfsFS)
	rul_rawds <- rxImport(inData = rul_raw)
	names(rul_rawds) <- "RUL"
	test_df <- cbind(test_ds, rul_rawds)
	
# predict on the test data using the model
	predictions <- rxPredict(modelObject = bt, data = test_df)
	
# define evaluation function that computes error metrics
	evaluate_model <- function(observed, predicted) {
	  mean_observed <- mean(observed)
	  se <- (observed - predicted)^2
	  ae <- abs(observed - predicted)
	  sem <- (observed - mean_observed)^2
	  aem <- abs(observed - mean_observed)
	  mae <- mean(ae)
	  rmse <- sqrt(mean(se))
	  rae <- sum(ae) / sum(aem)
	  rse <- sum(se) / sum(sem)
	  rsq <- 1 - rse
	  metrics <- c("Mean Absolute Error" = mae,
	               "Root Mean Squared Error" = rmse,
	               "Relative Absolute Error" = rae,
	               "Relative Squared Error" = rse,
	               "Coefficient of Determination" = rsq)
	  return(metrics)
	}

# evaluate how well the prediction did compared to ground truth using the evaluation function
	boosted_metrics <- evaluate_model(
		observed = test_df$RUL,
	  predicted = predictions$RUL_Pred)
# root mean square error is 27.3259527
    
# save the model to local file system, not hdfs
  saveRDS(bt, "/home/pliu/btmodel.rds")
  
# print out result dataframe    
	result <- cbind(test_df$RUL, predictions$RUL_Pred)
	result_df <- as.data.frame(result)
	colnames <- c("RUL", "RUL_Pred")
	names(result_df) <- colnames
	result_df
