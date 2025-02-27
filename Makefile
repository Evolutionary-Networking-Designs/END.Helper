.PHONY : clean debug release	

build: clean debug release

clean:
	rm -rf Output

debug:
	dotnet build -c Debug
	dotnet pack -c Debug

release:
	dotnet build -c Release
	dotnet pack -c Release